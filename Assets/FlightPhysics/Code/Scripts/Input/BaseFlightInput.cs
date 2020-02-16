namespace FlightPhysics.Input
{
    using UnityEngine;

    public class BaseFlightInput : MonoBehaviour
    {

        #region Fields

        protected float pitch = 0f;
        protected float roll = 0f;
        protected float yaw = 0f;
        protected float throttle = 0f;
        protected int flaps = 0;
        public int MaxFlapIncrements = 2;
        protected float brake = 0f;
        protected float ThrottleSpeed = 0.06f;
        private float _stickyThrottle;
        public KeyCode BrakeKey = KeyCode.Space;

        [SerializeField]
        private KeyCode _cameraKey = KeyCode.X;
        protected bool _CameraSwitch = false;

        #endregion

        #region Properties

        public float Pitch { get { return pitch; } }
        public float Roll { get { return roll; } }
        public float Yaw { get { return yaw; } }
        public float Throttle { get { return throttle; } }
        public int Flaps { get { return flaps; } }
        public float Brake { get { return brake; } }
        public float StickyThrottle { get { return _stickyThrottle; } }
        public bool CameraSwitch { get { return _CameraSwitch; } }
        public float NormalizedFlaps { get { return (float)flaps / MaxFlapIncrements; } }

        #endregion

        #region BuiltIn Methods

        public virtual void Start()
        {

        }

        public virtual void Update()
        {
            HandleInput();
        }

        #endregion

        #region Custom Methods

        protected virtual void HandleInput()
        {
            //Main controls
            pitch = Input.GetAxis("Vertical");
            roll = Input.GetAxis("Horizontal");
            yaw = Input.GetAxis("Yaw");
            throttle = Input.GetAxis("Throttle");
            ThrottleControl();

            //Brakes
            brake = Input.GetKey(BrakeKey) ? 1f : 0f;

            //Flaps
            if (Input.GetKeyDown(KeyCode.C))
            {
                flaps += 1;
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                flaps -= 1;
            }

            flaps = Mathf.Clamp(flaps, 0, MaxFlapIncrements);


            _CameraSwitch = Input.GetKeyDown(_cameraKey);
        }

        protected virtual void ThrottleControl()
        {
            _stickyThrottle = _stickyThrottle + (Throttle * ThrottleSpeed
                * Time.deltaTime);

            _stickyThrottle = Mathf.Clamp01(_stickyThrottle);
        }

        #endregion

    }
}

