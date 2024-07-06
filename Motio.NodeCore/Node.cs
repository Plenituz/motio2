using Motio.ObjectStoring;
using System;
using Motio.NodeCore.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Motio.Debuging;

namespace Motio.NodeCore
{
    public abstract class Node : INotifyPropertyChanged
    {
        public const string PROPERTY_OUT_CHANNEL = "propertyOut";
        public const string MESH_CHANNEL = "mesh";
        public const string PATH_CHANNEL = "path";
        public const string POLYGON_CHANNEL = "polygon";
        /// <summary>
        /// don't use this channel, it's used by the caching system
        /// </summary>
        public const string TRIANGULATION_RESULT_CHANNEL = "triangulation";

        private NodeUUID _uuid;

        public event PropertyChangedEventHandler PropertyChanged;

        [SaveMe]
        public NodeUUID UUID
        {
            get
            {
                //not a one liner for clarity since NodeUUID has 3 args
                if (_uuid == null)
                    _uuid = new NodeUUID(this, this.GetTimeline().uuidGroup);
                return _uuid;
            }
            set => _uuid = value;
        }
        /// <summary>
        /// all the properties of this node
        /// </summary>
        [SaveMe]
        public PropertyGroup Properties { get; set; }
        /// <summary>
        /// these are the tools that will be displayed in the property panel
        /// only one can be active at a time (in the entire software)
        /// </summary>
        public ObservableCollection<NodeTool> Tools { get; set; } = new ObservableCollection<NodeTool>();
        /// <summary>
        /// theses are the tools that will get activated as soon as the node is in
        /// the property panel. their can be several passive tools activated at the same time
        /// </summary>
        public ObservableCollection<NodeTool> PassiveTools { get; set; } = new ObservableCollection<NodeTool>();


        [SaveMe]
        public virtual string UserGivenName { get; set; }
        [SaveMe]
        public bool Enabled { get; set; } = true;

        protected Node()
        {
            Properties = new PropertyGroup(this);
            SetupNode();
        }

        protected void AfterConstructor()
        {
            PropertyChanged += Node_PropertyChanged;
        }

        private void Node_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            EventHall.Trigger(this, UUID.ToString(), e.PropertyName);
        }

        /// <summary>
        /// you should override this method to setup the properties and not do it in the constructor
        /// this is only called if creating a timeline, not while loading a file
        /// </summary>
        public abstract void SetupProperties();
        /// <summary>
        /// this is called before SetupProperties at node creation, even when loading a file 
        /// </summary>
        protected virtual void SetupNode()
        {

        }

        public virtual void Delete()
        {
            this.FindGraphicsNode(out _).StopBackgroundProcessing();
            Properties.Delete();
            for (int i = 0; i < Tools.Count; i++)
            {
                Tools[i].Delete();
            }
            for (int i = 0; i < PassiveTools.Count; i++)
            {
                PassiveTools[i].Delete();
            }
        }

        /// <summary>
        /// this verify if the value given is good, if it's not it will return an exception
        /// otherwise null
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Exception ValidatePropertyValue(NodePropertyBase property, object value)
        {
            return null;
        }
    }
}
