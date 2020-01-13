using UnityEngine;
using System.Collections;

namespace MapMagic
{
	public class InstantUpdater : MonoBehaviour 
	{
		#if RTP

		private ReliefTerrain rtp;
		
		public bool enabledEditor = true;
		public bool enabledPlaymode;

		public void Refresh ()
		{
			if (rtp==null) rtp = MapMagic.instance.GetComponent<ReliefTerrain>();

			foreach (Chunk chunk in MapMagic.instance.chunks.All())
			{
				Material mat = chunk.terrain.materialTemplate;

				rtp.RefreshTextures(mat);
				rtp.globalSettingsHolder.Refresh(mat, rtp);
			}
		}

		public void Update ()
		{
			if (enabledPlaymode) Refresh();
		}

		#endif

	}


}
