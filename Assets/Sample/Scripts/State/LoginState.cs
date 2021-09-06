using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace EosMLAPITransports.Sample
{
	public class LoginState : StateBase
	{
		public override void Run(object prm)
		{
			var type = (LoginCredentialType)prm;
			if (type == LoginCredentialType.Developer)
			{
				UIStack.Switch("UIDevLogin", new DevLoginViewModel
				{
					OnLogin = (vm) => Login(LoginCredentialType.Developer, vm.Host, vm.UserName)
				});
			}
			else if (type == LoginCredentialType.AccountPortal)
			{
				Login(LoginCredentialType.AccountPortal, "", "");
			}
		}

		async void Login(LoginCredentialType loginType, string id, string token)
		{
			try
			{
				await StartLogin(loginType, id, token);
				Switch<LobbySelectState>();
			}
			catch (Exception ex)
			{
				Debug.LogError(ex);
			}
		}

		// 下の処理はSDKがやってほしい

		Task StartLogin(LoginCredentialType loginType, string id, string token)
		{
			var future = new TaskCompletionSource<bool>();
			EOS.StartLoginWithLoginTypeAndToken(loginType, id, token, (info) =>
			{
				if (info.ResultCode == Result.Success)
				{
					ConnectLogin(future, info);
				}
				else if (info.ResultCode == Result.InvalidUser)
				{
					EOS.AuthLinkExternalAccountWithContinuanceToken(info.ContinuanceToken, LinkAccountFlags.NoFlags, (LinkAccountCallbackInfo linkAccountCallbackInfo) =>
					{
						ConnectLogin(future, info);
					});
				}
				else
				{
					future.TrySetException(new Exception($"login fail {info.ResultCode} : {info}"));
				}
			});
			return future.Task;
		}

		void ConnectLogin(TaskCompletionSource<bool> future, LoginCallbackInfo loginCallbackInfo)
		{
			EOS.StartConnectLoginWithEpicAccount(loginCallbackInfo.LocalUserId, (Epic.OnlineServices.Connect.LoginCallbackInfo connectLoginCallbackInfo) =>
			{
				if (connectLoginCallbackInfo.ResultCode == Result.Success)
				{
					future.TrySetResult(true);
				}
				else if (connectLoginCallbackInfo.ResultCode == Result.InvalidUser)
				{
					EOS.CreateConnectUserWithContinuanceToken(connectLoginCallbackInfo.ContinuanceToken, (Epic.OnlineServices.Connect.CreateUserCallbackInfo createUserCallbackInfo) =>
					{
						EOS.StartConnectLoginWithEpicAccount(loginCallbackInfo.LocalUserId, (Epic.OnlineServices.Connect.LoginCallbackInfo retryConnectLoginCallbackInfo) =>
						{
							if (retryConnectLoginCallbackInfo.ResultCode == Result.Success)
							{
								future.TrySetResult(true);
							}
							else
							{
								future.TrySetException(new Exception($"login fail {retryConnectLoginCallbackInfo.ResultCode} : {retryConnectLoginCallbackInfo}"));
							}
						});
					});
				}
				else
				{
					future.TrySetException(new Exception($"login fail {connectLoginCallbackInfo.ResultCode} : {connectLoginCallbackInfo}"));
				}
			});
		}
	}
}

