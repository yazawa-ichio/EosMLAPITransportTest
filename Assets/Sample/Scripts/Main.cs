using ILib.UI;
using System.Collections.Generic;
using UnityEngine;

namespace EosMLAPITransports.Sample
{


	public class Main : MonoBehaviour
	{

		[SerializeField]
		UIStack m_UIStack;

		public UIStack UIStack => m_UIStack;

		Stack<StateBase> m_GameState = new Stack<StateBase>();

		void Start()
		{
			Switch<TitleState>(null, null);
		}

		public void Switch<T>(StateBase current, object prm) where T : StateBase
		{
			while (m_GameState.Count > 0)
			{
				bool retry = current != m_GameState.Peek();
				Destroy(m_GameState.Pop());
				if (!retry)
				{
					break;
				}
			}
			var state = gameObject.AddComponent<T>();
			m_GameState.Push(state);
			state.Run(prm);
		}

	}

}