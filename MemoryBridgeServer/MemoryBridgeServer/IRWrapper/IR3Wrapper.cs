using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace MemoryBridgeServer
{
    public class IR3Wrapper
    {
        private static bool isWrapped;
        private static bool? hasAssembly = null;

        protected internal static Type IR3ServoControllerType { get; set; }
        protected internal static Type IR3ControlGroupType { get; set; }
        protected internal static Type IR3ServoType { get; set; }
        protected internal static Type IR3ServoPartType { get; set; }
        protected internal static Type IR3ServoMechanismType { get; set; }
        protected internal static Type IR3ServoMotorType { get; set; }
        protected internal static object ActualServoController { get; set; }

        internal static IR3API IR3Controller { get; set; }
        internal static bool AssemblyExists { get { return (IR3ServoControllerType != null); } }
        internal static bool InstanceExists { get { return (IR3Controller != null); } }
        internal static bool APIReady { get { return hasAssembly.HasValue && hasAssembly.Value && isWrapped && IR3Controller.Ready; } }

        internal static Type GetType(string name)
        {
            Type type = null;
            AssemblyLoader.loadedAssemblies.TypeOperation(t =>
            {
                if (t.FullName == name)
                    type = t;
            });
            return type;
        }

        internal static bool InitWrapper()
        {
            // Prevent the init function from continuing to initialize if InfernalRobotics is not installed.
            if (hasAssembly == null)
            {
                LogFormatted("Attempting to Grab IR3 Assembly...");
                hasAssembly = AssemblyLoader.loadedAssemblies.Any(a => a.dllName.Equals("InfernalRobotics_v3"));
                if (hasAssembly.Value)
                    LogFormatted("Found IR3 Assembly!");
                else
                    LogFormatted("Did not find IR3 Assembly.");
            }
            if (!hasAssembly.Value)
            {
                isWrapped = false;
                return isWrapped;
            }

            isWrapped = false;
            ActualServoController = null;
            IR3Controller = null;
            LogFormatted("Attempting to Grab IR3 Types...");

            IR3ServoControllerType = GetType("InfernalRobotics_v3.Command.Controller");

            if (IR3ServoControllerType == null)
            {
                return false;
            }

            LogFormatted("IR3 Version:{0}", IR3ServoControllerType.Assembly.GetName().Version.ToString());

            IR3ServoMechanismType = GetType("InfernalRobotics_v3.Control.IServo");

            if (IR3ServoMechanismType == null)
            {
                LogFormatted("[IR3 Wrapper] Failed to grab Mechanism Type");
                return false;
            }

            IR3ServoMotorType = GetType("InfernalRobotics_v3.Control.IMotor");

            if (IR3ServoMotorType == null)
            {
                LogFormatted("[IR3 Wrapper] Failed to grab ServoMotor Type");
                return false;
            }

            IR3ServoType = GetType("InfernalRobotics_v3.Control.IServo");

            if (IR3ServoType == null)
            {
                LogFormatted("[IR3 Wrapper] Failed to grab Servo Type");
                return false;
            }

            IR3ServoPartType = GetType("InfernalRobotics_v3.Control.IServo");

            if (IR3ServoType == null)
            {
                LogFormatted("[IR3 Wrapper] Failed to grab ServoPart Type");
                return false;
            }

            IR3ControlGroupType = GetType("InfernalRobotics_v3.Command.ControlGroup");

            if (IR3ControlGroupType == null)
            {
                var IR3assembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.FullName.Contains("InfernalRobotics_v3"));
                if (IR3assembly == null)
                {
                    LogFormatted("[IR3 Wrapper] cannot find InfernalRobotics_v3.dll");
                    return false;
                }
                foreach (Type t in IR3assembly.assembly.GetExportedTypes())
                {
                    LogFormatted("[IR3 Wrapper] Exported type: " + t.FullName);
                }

                LogFormatted("[IR3 Wrapper] Failed to grab ControlGroup Type");
                return false;
            }

            LogFormatted("Got Assembly Types, grabbing Instance");

            try
            {
                var propertyInfo = IR3ServoControllerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);

                if (propertyInfo == null)
                    LogFormatted("[IR3 Wrapper] Cannot find Instance Property");
                else
                    ActualServoController = propertyInfo.GetValue(null, null);
            }
            catch (Exception e)
            {
                LogFormatted("No Instance found, " + e.Message);
            }

            if (ActualServoController == null)
            {
                LogFormatted("Failed grabbing Instance");
                return false;
            }

            LogFormatted("Got Instance, Creating Wrapper Objects");
            IR3Controller = new InfernalRoboticsAPI();
            isWrapped = true;
            return true;
        }

        #region Private Implementation

        private class InfernalRoboticsAPI : IR3API
        {
            private PropertyInfo apiReady;
            private object actualServoGroups;

            public InfernalRoboticsAPI()
            {
                DetermineReady();
                BuildServoGroups();
            }

            private void BuildServoGroups()
            {
                var servoGroupsField = IR3ServoControllerType.GetField("ServoGroups");
                if (servoGroupsField == null)
                    LogFormatted("Failed Getting ServoGroups fieldinfo");
                else if (IR3Wrapper.ActualServoController == null)
                {
                    LogFormatted("ServoController Instance not found");
                }
                else
                {
                    actualServoGroups = servoGroupsField.GetValue(IR3Wrapper.ActualServoController);
                }
            }

            private void DetermineReady()
            {
                LogFormatted("Getting APIReady Object");
                apiReady = IR3ServoControllerType.GetProperty("APIReady", BindingFlags.Public | BindingFlags.Static);
                LogFormatted("Success: " + (apiReady != null));
            }

            public bool Ready
            {
                get
                {
                    if (apiReady == null || actualServoGroups == null)
                        return false;

                    return (bool)apiReady.GetValue(null, null);
                }
            }

            public IList<IControlGroup> ServoGroups
            {
                get
                {
                    BuildServoGroups();
                    return ExtractServoGroups(actualServoGroups);
                }
            }

            private IList<IControlGroup> ExtractServoGroups(object servoGroups)
            {
                var listToReturn = new List<IControlGroup>();

                if (servoGroups == null)
                    return listToReturn;

                try
                {
                    //iterate each "value" in the dictionary
                    foreach (var item in (IList)servoGroups)
                    {
                        listToReturn.Add(new IR3ControlGroup(item));
                    }
                }
                catch (Exception ex)
                {
                    LogFormatted("Cannot list ServoGroups: {0}", ex.Message);
                }
                return listToReturn;
            }
        }

        private class IR3ControlGroup : IControlGroup
        {
            private readonly object actualControlGroup;

            private PropertyInfo nameProperty;
            private PropertyInfo vesselProperty;
            private PropertyInfo forwardKeyProperty;
            private PropertyInfo expandedProperty;
            private PropertyInfo speedProperty;
            private PropertyInfo reverseKeyProperty;

            private MethodInfo moveRightMethod;
            private MethodInfo moveLeftMethod;
            private MethodInfo moveCenterMethod;
            private MethodInfo moveNextPresetMethod;
            private MethodInfo movePrevPresetMethod;
            private MethodInfo stopMethod;

            public IR3ControlGroup(object cg)
            {
                actualControlGroup = cg;
                FindProperties();
                FindMethods();
            }

            private void FindProperties()
            {
                nameProperty = IR3ControlGroupType.GetProperty("Name");
                vesselProperty = IR3ControlGroupType.GetProperty("Vessel");
                forwardKeyProperty = IR3ControlGroupType.GetProperty("ForwardKey");
                reverseKeyProperty = IR3ControlGroupType.GetProperty("ReverseKey");
                speedProperty = IR3ControlGroupType.GetProperty("Speed");
                expandedProperty = IR3ControlGroupType.GetProperty("Expanded");

                var servosProperty = IR3ControlGroupType.GetProperty("Servos");
                ActualServos = servosProperty.GetValue(actualControlGroup, null);
            }

            private void FindMethods()
            {
                moveRightMethod = IR3ControlGroupType.GetMethod("MoveRight", BindingFlags.Public | BindingFlags.Instance);
                moveLeftMethod = IR3ControlGroupType.GetMethod("MoveLeft", BindingFlags.Public | BindingFlags.Instance);
                moveCenterMethod = IR3ControlGroupType.GetMethod("MoveCenter", BindingFlags.Public | BindingFlags.Instance);
                moveNextPresetMethod = IR3ControlGroupType.GetMethod("MoveNextPreset", BindingFlags.Public | BindingFlags.Instance);
                movePrevPresetMethod = IR3ControlGroupType.GetMethod("MovePrevPreset", BindingFlags.Public | BindingFlags.Instance);
                stopMethod = IR3ControlGroupType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
            }

            public string Name
            {
                get { return (string)nameProperty.GetValue(actualControlGroup, null); }
                set { nameProperty.SetValue(actualControlGroup, value, null); }
            }

            public Vessel Vessel
            {
                get { return vesselProperty != null ? (Vessel)vesselProperty.GetValue(actualControlGroup, null) : null; }
            }

            public string ForwardKey
            {
                get { return (string)forwardKeyProperty.GetValue(actualControlGroup, null); }
                set { forwardKeyProperty.SetValue(actualControlGroup, value, null); }
            }

            public string ReverseKey
            {
                get { return (string)reverseKeyProperty.GetValue(actualControlGroup, null); }
                set { reverseKeyProperty.SetValue(actualControlGroup, value, null); }
            }

            public float Speed
            {
                get { return (float)speedProperty.GetValue(actualControlGroup, null); }
                set { speedProperty.SetValue(actualControlGroup, value, null); }
            }

            public bool Expanded
            {
                get { return (bool)expandedProperty.GetValue(actualControlGroup, null); }
                set { expandedProperty.SetValue(actualControlGroup, value, null); }
            }

            private object ActualServos { get; set; }

            public IList<IServo> Servos
            {
                get
                {
                    return ExtractServos(ActualServos);
                }
            }

            public void MoveRight()
            {
                moveRightMethod.Invoke(actualControlGroup, new object[] { });
            }

            public void MoveLeft()
            {
                moveLeftMethod.Invoke(actualControlGroup, new object[] { });
            }

            public void MoveCenter()
            {
                moveCenterMethod.Invoke(actualControlGroup, new object[] { });
            }

            public void MoveNextPreset()
            {
                moveNextPresetMethod.Invoke(actualControlGroup, new object[] { });
            }

            public void MovePrevPreset()
            {
                movePrevPresetMethod.Invoke(actualControlGroup, new object[] { });
            }

            public void Stop()
            {
                stopMethod.Invoke(actualControlGroup, new object[] { });
            }

            private IList<IServo> ExtractServos(object actualServos)
            {
                var listToReturn = new List<IServo>();

                if (actualServos == null)
                    return listToReturn;

                try
                {
                    //iterate each key in the dictionary
                    foreach (var item in ((IList)actualServos))
                    {
                        listToReturn.Add(new IR3Servo(item));
                    }
                }
                catch (Exception ex)
                {
                    LogFormatted("Error extracting from actualServos: {0}", ex.Message);
                }
                return listToReturn;
            }

            public bool Equals(IControlGroup other)
            {
                var controlGroup = other as IR3ControlGroup;
                return controlGroup != null && Equals(controlGroup);
            }
        }

        public class IR3Servo : IServo
        {
            private object actualServoMotor;

            private PropertyInfo maxConfigPositionProperty;
            private PropertyInfo minPositionProperty;
            private PropertyInfo maxPositionProperty;
            private PropertyInfo configSpeedProperty;
            private PropertyInfo speedProperty;
            private PropertyInfo currentSpeedProperty;
            private PropertyInfo accelerationProperty;
            private PropertyInfo isMovingProperty;
            private PropertyInfo isFreeMovingProperty;
            private PropertyInfo isLockedProperty;
            private PropertyInfo isAxisInvertedProperty;
            private PropertyInfo nameProperty;
            private PropertyInfo highlightProperty;
            private PropertyInfo positionProperty;
            private PropertyInfo minConfigPositionProperty;

            private PropertyInfo UIDProperty;
            private PropertyInfo HostPartProperty;

            private MethodInfo moveRightMethod;
            private MethodInfo moveLeftMethod;
            private MethodInfo moveCenterMethod;
            private MethodInfo moveNextPresetMethod;
            private MethodInfo movePrevPresetMethod;
            private MethodInfo moveToMethod;
            private MethodInfo stopMethod;

            public IR3Servo(object s)
            {
                actualServo = s;

                FindProperties();
                FindMethods();
            }

            private void FindProperties()
            {
                nameProperty = IR3ServoPartType.GetProperty("Name");
                highlightProperty = IR3ServoPartType.GetProperty("Highlight");
                UIDProperty = IR3ServoPartType.GetProperty("UID");
                HostPartProperty = IR3ServoPartType.GetProperty("HostPart");

                var motorProperty = IR3ServoType.GetProperty("Motor");
                actualServoMotor = motorProperty.GetValue(actualServo, null);

                positionProperty = IR3ServoMechanismType.GetProperty("Position");
                minPositionProperty = IR3ServoMechanismType.GetProperty("MinPositionLimit");
                maxPositionProperty = IR3ServoMechanismType.GetProperty("MaxPositionLimit");

                minConfigPositionProperty = IR3ServoMechanismType.GetProperty("MinPosition");
                maxConfigPositionProperty = IR3ServoMechanismType.GetProperty("MaxPosition");

                isMovingProperty = IR3ServoMechanismType.GetProperty("IsMoving");
                isFreeMovingProperty = IR3ServoMechanismType.GetProperty("IsFreeMoving");
                isLockedProperty = IR3ServoMechanismType.GetProperty("IsLocked");

                speedProperty = IR3ServoMotorType.GetProperty("SpeedLimit");
                configSpeedProperty = IR3ServoMotorType.GetProperty("DefaultSpeed");
                currentSpeedProperty = IR3ServoMotorType.GetProperty("Speed");
                accelerationProperty = IR3ServoMotorType.GetProperty("AccelerationLimit");
                isAxisInvertedProperty = IR3ServoMotorType.GetProperty("IsAxisInverted");
            }

            private void FindMethods()
            {
                moveRightMethod = IR3ServoMotorType.GetMethod("MoveRight", BindingFlags.Public | BindingFlags.Instance);
                moveLeftMethod = IR3ServoMotorType.GetMethod("MoveLeft", BindingFlags.Public | BindingFlags.Instance);
                moveCenterMethod = IR3ServoMotorType.GetMethod("MoveCenter", BindingFlags.Public | BindingFlags.Instance);
                moveNextPresetMethod = IR3ServoMotorType.GetMethod("MoveNextPreset", BindingFlags.Public | BindingFlags.Instance);
                movePrevPresetMethod = IR3ServoMotorType.GetMethod("MovePrevPreset", BindingFlags.Public | BindingFlags.Instance);
                stopMethod = IR3ServoMotorType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
                moveToMethod = IR3ServoMotorType.GetMethod("MoveTo", new[] { typeof(float), typeof(float) });
            }

            private readonly object actualServo;

            public string Name
            {
                get { return (string)nameProperty.GetValue(actualServo, null); }
                set { nameProperty.SetValue(actualServo, value, null); }
            }

            public uint UID
            {
                get { return (uint)UIDProperty.GetValue(actualServo, null); }
            }

            public Part HostPart
            {
                get { return (Part)HostPartProperty.GetValue(actualServo, null); }
            }

            public bool Highlight
            {
                //get { return (bool)HighlightProperty.GetValue(actualServo, null); }
                set { highlightProperty.SetValue(actualServo, value, null); }
            }

            public float Position
            {
                get { return (float)positionProperty.GetValue(actualServo, null); }
            }

            public float MinConfigPosition
            {
                get { return (float)minConfigPositionProperty.GetValue(actualServo, null); }
            }

            public float MaxConfigPosition
            {
                get { return (float)maxConfigPositionProperty.GetValue(actualServo, null); }
            }

            public float MinPosition
            {
                get { return (float)minPositionProperty.GetValue(actualServo, null); }
                set { minPositionProperty.SetValue(actualServo, value, null); }
            }

            public float MaxPosition
            {
                get { return (float)maxPositionProperty.GetValue(actualServo, null); }
                set { maxPositionProperty.SetValue(actualServo, value, null); }
            }

            public float ConfigSpeed
            {
                get { return (float)configSpeedProperty.GetValue(actualServoMotor, null); }
            }

            public float Speed
            {
                get { return (float)speedProperty.GetValue(actualServoMotor, null); }
                set { speedProperty.SetValue(actualServoMotor, value, null); }
            }

            public float CurrentSpeed
            {
                get { return (float)currentSpeedProperty.GetValue(actualServoMotor, null); }
                set { currentSpeedProperty.SetValue(actualServoMotor, value, null); }
            }

            public float Acceleration
            {
                get { return (float)accelerationProperty.GetValue(actualServoMotor, null); }
                set { accelerationProperty.SetValue(actualServoMotor, value, null); }
            }

            public bool IsMoving
            {
                get { return (bool)isMovingProperty.GetValue(actualServo, null); }
            }

            public bool IsFreeMoving
            {
                get { return (bool)isFreeMovingProperty.GetValue(actualServo, null); }
            }

            public bool IsLocked
            {
                get { return (bool)isLockedProperty.GetValue(actualServo, null); }
                set { isLockedProperty.SetValue(actualServo, value, null); }
            }

            public bool IsAxisInverted
            {
                get { return (bool)isAxisInvertedProperty.GetValue(actualServoMotor, null); }
                set { isAxisInvertedProperty.SetValue(actualServoMotor, value, null); }
            }

            public void MoveRight()
            {
                moveRightMethod.Invoke(actualServoMotor, new object[] { });
            }

            public void MoveLeft()
            {
                moveLeftMethod.Invoke(actualServoMotor, new object[] { });
            }

            public void MoveCenter()
            {
                moveCenterMethod.Invoke(actualServoMotor, new object[] { });
            }

            public void MoveNextPreset()
            {
                moveNextPresetMethod.Invoke(actualServoMotor, new object[] { });
            }

            public void MovePrevPreset()
            {
                movePrevPresetMethod.Invoke(actualServoMotor, new object[] { });
            }

            public void MoveTo(float position, float speed)
            {
                moveToMethod.Invoke(actualServoMotor, new object[] { position, speed });
            }

            public void Stop()
            {
                stopMethod.Invoke(actualServoMotor, new object[] { });
            }

            public bool Equals(IServo other)
            {
                var servo = other as IR3Servo;
                return servo != null && Equals(servo);
            }

            public override bool Equals(object o)
            {
                var servo = o as IR3Servo;
                return servo != null && actualServo.Equals(servo.actualServo);
            }

            public override int GetHashCode()
            {
                return (actualServo != null ? actualServo.GetHashCode() : 0);
            }

            public static bool operator ==(IR3Servo left, IR3Servo right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(IR3Servo left, IR3Servo right)
            {
                return !Equals(left, right);
            }

            protected bool Equals(IR3Servo other)
            {
                return Equals(actualServo, other.actualServo);
            }
        }

        #endregion Private Implementation

        #region API Contract

        public interface IR3API
        {
            bool Ready { get; }

            IList<IControlGroup> ServoGroups { get; }
        }

        public interface IControlGroup : IEquatable<IControlGroup>
        {
            string Name { get; set; }

            //can only be used in Flight, null checking is mandatory
            Vessel Vessel { get; }

            string ForwardKey { get; set; }

            string ReverseKey { get; set; }

            float Speed { get; set; }

            bool Expanded { get; set; }

            IList<IServo> Servos { get; }

            void MoveRight();

            void MoveLeft();

            void MoveCenter();

            void MoveNextPreset();

            void MovePrevPreset();

            void Stop();
        }

        public interface IServo : IEquatable<IServo>
        {
            string Name { get; set; }

            uint UID { get; }

            Part HostPart { get; }

            bool Highlight { set; }

            float Position { get; }

            float MinConfigPosition { get; }

            float MaxConfigPosition { get; }

            float MinPosition { get; set; }

            float MaxPosition { get; set; }

            float ConfigSpeed { get; }

            float Speed { get; set; }

            float CurrentSpeed { get; set; }

            float Acceleration { get; set; }

            bool IsMoving { get; }

            bool IsFreeMoving { get; }

            bool IsLocked { get; set; }

            bool IsAxisInverted { get; set; }

            void MoveRight();

            void MoveLeft();

            void MoveCenter();

            void MoveNextPreset();

            void MovePrevPreset();

            void MoveTo(float position, float speed);

            void Stop();

            bool Equals(object o);

            int GetHashCode();
        }

        #endregion API Contract

        #region Logging Stuff

        /// <summary>
        /// Some Structured logging to the debug file - ONLY RUNS WHEN DLL COMPILED IN DEBUG MODE
        /// </summary>
        /// <param name="message">Text to be printed - can be formatted as per string.format</param>
        /// <param name="strParams">Objects to feed into a string.format</param>
        [System.Diagnostics.Conditional("DEBUG")]
        internal static void LogFormatted_DebugOnly(string message, params object[] strParams)
        {
            LogFormatted(message, strParams);
        }

        /// <summary>
        /// Some Structured logging to the debug file
        /// </summary>
        /// <param name="message">Text to be printed - can be formatted as per string.format</param>
        /// <param name="strParams">Objects to feed into a string.format</param>
        internal static void LogFormatted(string message, params object[] strParams)
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            message = string.Format(message, strParams);

            string strMessageLine = declaringType != null ?
                string.Format("{0},{2}-{3},{1}", DateTime.Now, message, assemblyName, declaringType.Name) :
                string.Format("{0},{2}-NO-DECLARE,{1}", DateTime.Now, message, assemblyName);

            UnityEngine.Debug.Log(strMessageLine);
        }

        #endregion Logging Stuff
    }
}
