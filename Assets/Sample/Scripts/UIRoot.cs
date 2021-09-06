using ILib.UI;
using System.Threading.Tasks;
using UVMBinding;

namespace EosMLAPITransports.Sample
{
	public class UIRoot : UIControl<ViewModel>
	{
		View m_View;
		protected override Task OnCreated(ViewModel prm)
		{
			m_View = GetComponent<View>();
			m_View.Attach(prm);
			return base.OnCreated(prm);
		}
	}
}