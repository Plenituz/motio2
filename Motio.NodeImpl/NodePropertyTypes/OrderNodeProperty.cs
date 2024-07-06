using Motio.NodeCore;
using Motio.ObjectStoring;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Motio.NodeImpl.NodePropertyTypes
{
    public class OrderNodeProperty : NodePropertyBase
    {
        public override bool IsKeyframable => false;

        [SaveMe]
        public ObservableCollection<string> items;
        public override object StaticValue
        {
            get => "";
            set => throw new InvalidOperationException("can't set static value of OrderNodeProperty, use 'items' directly");
        }

        [OnDoneLoading]
        void DoneLoading()
        {
            items.CollectionChanged += Items_CollectionChanged;
        }

        public OrderNodeProperty(Node nodeHost) : base(nodeHost)
        {
            
        }

        public OrderNodeProperty(Node nodeHost, string description, string name, IEnumerable<string> items)
            : base(nodeHost, description, name)
        {
            this.items = new ObservableCollection<string>(items);
            this.items.CollectionChanged += Items_CollectionChanged;
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(StaticValue));
        }

        public override void CreateAnimationNode()
        {
            //none
        }
    }
}
