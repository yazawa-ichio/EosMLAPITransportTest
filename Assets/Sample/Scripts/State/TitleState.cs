using Epic.OnlineServices.Auth;

namespace EosMLAPITransports.Sample
{
	public class TitleState : StateBase
	{
		public override void Run(object prm)
		{
			UIStack.Switch("UITitle", new TitleViewModel
			{
				DevLogin = OnDevLogin,
				AccountLogin = OnAccountLogin,
			});
		}

		void OnDevLogin()
		{
			Switch<LoginState>(LoginCredentialType.Developer);
		}

		void OnAccountLogin()
		{
			Switch<LoginState>(LoginCredentialType.AccountPortal);
		}

	}
}

