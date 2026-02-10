using Autodesk.Navisworks.Api.Plugins;
using System.Windows.Forms;

namespace YourPluginNamespace
{
    [Plugin("NewFunctionCommand", "CMP", DisplayName = "Новая команда")]
    public class NewFunctionCommand : AddInPlugin
    {
        public override int Execute(params string[] parameters)
        {
            MessageBox.Show("Нажата новая кнопка", "Информация");
            return 0;
        }
    }
}