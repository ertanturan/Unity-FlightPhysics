using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LUI_Volume : MonoBehaviour {

		public void VolumeControl(float volumeControl) {
		AudioListener.volume = volumeControl; 
	}
}
