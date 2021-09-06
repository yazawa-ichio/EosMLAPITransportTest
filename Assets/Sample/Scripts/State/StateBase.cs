using ILib.UI;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace EosMLAPITransports.Sample
{
	public abstract class StateBase : MonoBehaviour
	{
		Main m_Main;

		protected virtual bool IsRoot => true;

		protected EOSManager.EOSSingleton EOS => EOSManager.Instance;

		protected UIStack UIStack => m_Main.UIStack;

		void Awake()
		{
			m_Main = GetComponent<Main>();
		}

		protected void Switch<T>(object prm = null) where T : StateBase
		{
			if (IsRoot && UIStack.Count > 0)
			{
				UIStack.Pop(UIStack.Count);
			}
			m_Main.Switch<T>(this, prm);
		}

		public virtual void Run(object prm) { }

	}
}

