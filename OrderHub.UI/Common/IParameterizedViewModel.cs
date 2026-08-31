using System.Threading.Tasks;

namespace OrderHub.UI.Common;

public interface IParameterizedViewModel
{
    Task Initialize(object parameter);
}