using Motio.NodeCore;
using Motio.ObjectStoring;
using PropertyChanged;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Motio.NodeImpl.NodePropertyTypes
{
    public class DropdownNodeProperty : NodePropertyBase
    {
        public override object StaticValue
        {
            get => selected;
            set => selected = value.ToString();
        }
        public override bool IsKeyframable => false;

        //TODO choices est rempli de "null" quand il se fait load depuis le json
        [SaveMe]
        public ObservableCollection<string> choices;
        private string selected;

        public DropdownNodeProperty(Node nodeHost) : base(nodeHost)
        {
        }

        public DropdownNodeProperty(Node nodeHost, string description, 
            string name, IEnumerable<string> choices) : base(nodeHost, description, name)
        {
            //on normalise the type pour quans il sera stocker en json 
            //on pourra recréer une list.
            //si on stock direct la data structure, ca pourrait etre une list python
            //que le TimelineLoader ne sait pas créer
            this.choices = new ObservableCollection<string>(choices);
        }

        public override void CreateAnimationNode()
        {
            
        }
    }
}
