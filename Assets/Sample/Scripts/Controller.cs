using MLAPI;
using UnityEngine;

namespace EosMLAPITransports.Sample
{
	public class Controller : NetworkBehaviour
	{
		public override void NetworkStart()
		{
			if (!IsOwner)
			{
				return;
			}
			transform.position = Vector3.zero;
		}
		void Update()
		{
			if (!IsOwner)
			{
				return;
			}
			var moveX = Input.GetAxis("Horizontal") * Time.deltaTime;
			var moveY = Input.GetAxis("Vertical") * Time.deltaTime;
			transform.Translate(moveX, 0, moveY);
		}
	}
}