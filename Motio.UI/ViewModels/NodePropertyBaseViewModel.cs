using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using Motio.NodeCore.Interfaces;
using Motio.NodeCore;
using Motio.Animation;
using Motio.UI.Utils;
using System.Windows.Controls;
using Motio.NodeCommon.ObjectStoringImpl;
using Motio.Debuging;
using Motio.UI.Views.TimelineDisplays;
using Motio.NodeImpl;
using Motio.NodeCommon.StandardInterfaces;
using Motio.ObjectStoring;
using Newtonsoft.Json.Linq;
using System;

namespace Motio.UI.ViewModels
{
    public abstract class NodePropertyBaseViewModel : Proxy<NodePropertyBase>, 
        INotifyPropertyChanged, IHasAttached<PropertyAffectingNodeViewModel>, IHasHost
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private NodeViewModel _host;
        public object Host          { get => _host; set => _host = (NodeViewModel)value; }
        public string Name          { get => Original.Name;        set => Original.Name = value; }
        public bool IsKeyframable         => Original.IsKeyframable;
        public bool HasErrors             => Original.HasErrors;
        public bool HasWarnings           => Original.HasWarnings;
        public IEnumerable<string> Errors => Original.Errors;
        public IEnumerable<string> Warnings => Original.Warnings;
        public string HostUniqueName => Original.HostUniqueName;
        [SaveMe]
        public bool Visible { get; set; } = true;
        public object StaticValue { get => Original.StaticValue; set => Original.StaticValue = value; }
        public string Description   { get => Original.Description; set => Original.Description = value; }
        public KeyframeHolder KeyframeHolder
        {
            get { return _original.FindAnimationNode()?.keyframeHolder; }
          //  set { }//set just used to trigger property changed by the weaver
        }
        public MainControlViewModel Root => this.FindMainViewModel();

        public ListProxy<PropertyAffectingNode, PropertyAffectingNodeViewModel> HiddenNodes { get; private set; }
        public ListProxy<PropertyAffectingNode, PropertyAffectingNodeViewModel> AttachedNodes { get; private set; }

        NodePropertyBase _original;
        public override NodePropertyBase Original => _original;

        /// <summary>
        /// do not cache the controls, so they ca be added to several property panels/node display at the same time
        /// </summary>
        public abstract ContentControl PropertyDisplay { get; }

        private DefaultTimelineDisplay _timelineDisplay;
        public virtual ContentControl TimelineDisplay => _timelineDisplay ?? (_timelineDisplay = new DefaultTimelineDisplay());

        public bool IsInTimeline => _host.PropertiesInTimeline.Contains(this);

        IEnumerable<PropertyAffectingNodeViewModel> IHasAttached<PropertyAffectingNodeViewModel>.AttachedMembers
        {
            get
            {
                foreach(PropertyAffectingNodeViewModel vm in AttachedNodes)
                    yield return vm;
            }
        }

        IEnumerable IHasAttached.AttachedMembers
        {
            get
            {
                foreach (var vm in AttachedNodes)
                    yield return vm;
            }
        }

        [CustomSaver]
        object CustomSaver()
        {
            object data = TimelineSaver.SaveObjectToJson(this, true);
            if (data == this)//if the object has no [SaveMe] the same object is returned
            {
                return this;
            }
            else
            {
                IDictionary dict = (IDictionary)data;
                dict.Add("uuid", _original.GetPath());
                return dict;
            }
        }

        [CreateLoadInstance]
        static object CreateLoadInstance(NodePropertyBase parent, Type type)
        {
            NodePropertyBaseViewModel vm = (NodePropertyBaseViewModel)Activator.CreateInstance(type, parent);
            return vm;
        }

        public NodePropertyBaseViewModel(NodePropertyBase property) : base(property)
        {
            this._original = property;
            _host = ProxyStatic.CreateProxy<NodeViewModel>(_original.nodeHost);
            HiddenNodes = new ListProxy<PropertyAffectingNode, PropertyAffectingNodeViewModel>(Original.hiddenNodes, Original.hiddenNodes);
            AttachedNodes = new ListProxy<PropertyAffectingNode, PropertyAffectingNodeViewModel>(Original.attachedNodes, Original.attachedNodes);
            property.PropertyChanged += Property_PropertyChanged;
            _original.PropertyChanged += _original_PropertyChanged;
        }

        private void _original_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        /// <summary>
        /// function called by PropertyChanged.Fody to trigger PropertyChanged
        /// also used to manually trigger PropertyChanged
        /// </summary>
        /// <param name="propertyName"></param>
        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Property_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //relay the event to the ui by replacing the first letter with a MAJ
            string name = e.PropertyName.Substring(0, 1).ToUpperInvariant() + e.PropertyName.Substring(1);
            PropertyChangedEventArgs args = new PropertyChangedEventArgs(name);
            PropertyChanged?.Invoke(this, args);
        }

        protected virtual void KeyframeHolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //trigger the Propertychanged by setting KeyframeHolder value
            if (e.PropertyName.Equals("keyframes") || e.PropertyName.Equals("Time"))
            {
                OnPropertyChanged(nameof(KeyframeHolder));
               // KeyframeHolder = null;
            }
            //ce qu'il y a en dessous marchait pas jsais pas pourquoi
            // PropertyChanged(sender, new PropertyChangedEventArgs("KeyframeHolder"));
        }

        public bool TryAttachNode(PropertyAffectingNode node)
        {
            return _original.TryAttachNode(node);
        }

        public void DisplayInTimeline()
        {
            _host.PropertiesInTimeline.Add(this);
        }

        public void HideInTimeline()
        {
            _host.PropertiesInTimeline.Remove(this);
        }

        public virtual void CreateAnimationNode()
        {
            _original.CreateAnimationNode();
            //manually listen to Keyframe holder changed event to trigger updates
            //TODO si le keyframe holder est supprimer et reajouter a 
            //l'interface yaura deux fois le property changed 
            //et si il est supprimé ca risque de faire des memory leaks
            KeyframeHolder.PropertyChanged += KeyframeHolder_PropertyChanged;
        }

        /// <summary>
        /// calculate (or get the cached value) the in value for this property (the value juste after the hidden nodes)
        /// this happens in the main thread because this kind of calculation will either be done by the background thread before
        /// or are very light and can be done fast
        /// </summary>
        /// <returns></returns>
        public object GetCurrentInValue()
        {
            AnimationTimelineViewModel tl = this.FindRoot() as AnimationTimelineViewModel;
            //crash if the tl is not found, can't recover so just let it crash, probably won't happen
            return _original.GetCacheOrCalculateInValue(tl.CurrentFrame).GetChannelData(Node.PROPERTY_OUT_CHANNEL);
        }

        public string GetPath()
        {
            return _original.GetPath();
        }

        public PropertyAffectingNodeViewModel CreateNode(ICreatableNode creatableNode)
        {
            PropertyAffectingNode node = creatableNode.CreateInstance() as PropertyAffectingNode;
            if (node == null)
            {
                Logger.WriteLine("node instance couldnt be created in AddNodeOfType (" + creatableNode.ClassNameStatic+ ")");
                return null;
            }

            bool attached = _original.TryAttachNode(node);
            if (!attached)
            {
                Logger.WriteLine("coudn't attach node " + node + " to " + _original);
                return null;
            }
            return ProxyStatic.GetProxyOf<PropertyAffectingNodeViewModel>(node);
        }

        public override void Delete()
        {
            base.Delete();
            _original.PropertyChanged -= Property_PropertyChanged;

            KeyframeHolder holder = KeyframeHolder;
            if(holder != null)
                holder.PropertyChanged -= KeyframeHolder_PropertyChanged;

            //foreach(PropertyAffectingNodeViewModel node in HiddenNodes)
            //{
            //    node.Original.Delete();
            //}
            //foreach (PropertyAffectingNodeViewModel node in AttachedNodes)
            //{
            //    node.Original.Delete();
            //}
            //don't call delete on original because this function is called 
            //as a result of the original being deleted
            //_original.Delete();
        }

        public void ReplaceMemberAt(int index, object member)
        {
            throw new System.NotImplementedException();
        }
    }
}
