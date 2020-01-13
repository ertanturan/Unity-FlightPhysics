using UnityEngine;

//ignoring ui in 5.2 build
#if UNITY_5_2
namespace MapMagicDemo
{
	public class GuiController : MonoBehaviour 
	{
		public void OnEnable() { Debug.LogWarning("MapMagic: Demo GUI Controller is not fully compatible with Unity 5.2"); }
	}
}
#else

using System.Collections.Generic;
using UnityEngine.UI;
//using Plugins;
using MapMagic;

namespace MapMagicDemo
{

	public class GuiController : MonoBehaviour 
	{
		private CharController _charController; public CharController charController { get{ if (_charController==null) _charController=FindObjectOfType<CharController>(); return _charController; }}
		private FlybyController _demoController; public FlybyController demoController { get{ if (_demoController==null) _demoController=FindObjectOfType<FlybyController>(); return _demoController; }}
		private CameraController _cameraController; public CameraController cameraController { get{ if (_cameraController==null) _cameraController=FindObjectOfType<CameraController>(); return _cameraController; }}
		private MapMagic.MapMagic _mapMagic; public MapMagic.MapMagic mapMagic { get{ if (_mapMagic==null) _mapMagic=FindObjectOfType<MapMagic.MapMagic>(); return _mapMagic; }}

		private int oldScreenWidth = 0;
		private int oldScreenHeight = 0;
		private float iconRotation = 0;
		public  float fpsUpdateInterval = 0.5f;
		private int  frames  = 0; // Frames drawn over the interval
		private float fpsTimeleft; // Left time for current interval
		private Vector3 camPos = new Vector3(0,-1,0);
		private float distTravalled;

		private Dictionary<string, Object> objects = new Dictionary<string, Object>();
		private T GetObject<T> (string name) where T : Object
		{
			if (objects.ContainsKey(name)) return (T)objects[name];

			Transform tfm = transform.FindChildRecursive(name); if (tfm == null) return null;
			T obj = tfm.GetComponent<T>(); 
			if (obj == null) return null;
			
			objects.Add(name, obj);
			return obj;
		}

		public void Update ()
		{
			//Graphic Raycaster prints error importing an asset, so MM shipped without it. Adding it on scen start
			if (gameObject.GetComponent<GraphicRaycaster>()==null) gameObject.AddComponent<GraphicRaycaster>();
			
			//switching fullscreen
			Toggle fullscreenToggle = GetObject<Toggle>("Fullscreen");
			if (Input.GetKeyDown("f")) fullscreenToggle.isOn = !fullscreenToggle.isOn;
			if (fullscreenToggle.isOn && !Screen.fullScreen)
			{
				oldScreenWidth = Screen.width;
				oldScreenHeight = Screen.height;
				Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
			}
			if (!fullscreenToggle.isOn && Screen.fullScreen)
			{
				Screen.SetResolution(oldScreenWidth, oldScreenHeight, false);
			}
			fullscreenToggle.isOn = Screen.fullScreen;

			//setting mouse look
			Toggle mouseToggle = GetObject<Toggle>("MouseLook");
			if (Input.GetKeyDown("m")) mouseToggle.isOn = !mouseToggle.isOn;
			cameraController.lockCursor = mouseToggle.isOn;

			//switching move modes
			if (Input.GetKeyDown("1") || GetObject<Toggle>("Walk").isOn)
			{
				charController.enabled = true; 
				charController.gravity = true; 
				charController.speed = 5; 
				charController.acceleration = 50; 
				demoController.enabled=false; 
				cameraController.follow=0;
				GetObject<Toggle>("Fly").isOn = false; GetObject<Toggle>("Demo").isOn = false;
			}

			if (Input.GetKeyDown("2") || GetObject<Toggle>("Fly").isOn)
			{
				charController.enabled = true; 
				charController.gravity = false; 
				charController.speed = 50; 
				charController.acceleration = 150; 
				demoController.enabled=false; 
				cameraController.follow=0;
				GetObject<Toggle>("Walk").isOn = false; GetObject<Toggle>("Demo").isOn = false;
			}

			if (Input.GetKeyDown("3") || GetObject<Toggle>("Demo").isOn)
			{
				charController.enabled = false; 
				demoController.enabled=true; 
				cameraController.follow=5;
				GetObject<Toggle>("Fly").isOn = false; GetObject<Toggle>("Walk").isOn = false;
			}
			

			//displaing generate mark
			GameObject generateMark = GetObject<RectTransform>("GenerateMark").gameObject;
			bool isWorking = ThreadWorker.IsWorking("MapMagic");
			if (!isWorking && generateMark.activeSelf) generateMark.SetActive(false);
			if (isWorking && !generateMark.activeSelf) generateMark.SetActive(true);

			if (generateMark.activeSelf) 
			{
				RectTransform icon = GetObject<RectTransform>("GenerateMarkIcon");
				iconRotation -= Time.deltaTime*100;
				iconRotation = iconRotation%360;
				icon.rotation = Quaternion.Euler(0,0,iconRotation);
			}


			//fps counter
			fpsTimeleft -= Time.deltaTime;
			++frames;

			if( fpsTimeleft <= 0.0 )
			{
				GetObject<Text>("FpsCounter").text = "FPS: " + frames/fpsUpdateInterval;
				frames = 0;
				fpsTimeleft = fpsUpdateInterval;
			}


			//travelled distance
			if (camPos.y < 0) camPos = Camera.main.transform.position;
			distTravalled += (Camera.main.transform.position - camPos).magnitude;
			camPos = Camera.main.transform.position;
			GetObject<Text>("DistTravalled").text = "Distance travelled:\n" + ((int)distTravalled);
		}
	}

}
#endif