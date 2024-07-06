using Motio.NodeCore;

namespace Motio.NodeCore
{
    /// <summary>
    /// same as PropertyGroup but doesn't call nodeHost.SetupProperties()
    /// </summary>
    public class PropertyGroupNoSetup : PropertyGroup
    {
        public PropertyGroupNoSetup(Node nodeHost) : base(nodeHost)
        {

        }

        protected override void CheckSetup()
        {
            //no check setup here
        }
    }
}
