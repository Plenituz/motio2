using Microsoft.Scripting.Hosting;
using Motio.Geometry;
using Motio.NodeCommon;
using Motio.NodeCore;
using Motio.NodeCore.Utils;
using Motio.NodeImpl.PropertyAffectingNodes;
using Motio.PythonRunning;
using System;
using System.ComponentModel;

namespace Motio.NodeImpl.NodePropertyTypes
{
    public class VectorNodeProperty : NodePropertyBase
    {
        public override bool IsKeyframable => true;
        public bool uniform = false;
        private float Ratio => vectorValue.Y / vectorValue.X;

        protected Vector2 vectorValue;
        protected ScriptScope pythonScope;

        public override object StaticValue
        {
            get => vectorValue;
            set
            {
                vectorValue = (Vector2)value;
               // OnPropertyChanged(nameof(StaticValue));
            }
        }

        public float X
        {
            get => vectorValue.X;
            set
            {
                if (uniform)
                {
                    vectorValue.Y += (value - vectorValue.X) * Ratio;
                    OnPropertyChanged(nameof(Y));
                }

                vectorValue.X = value;
              //  OnPropertyChanged(nameof(X));
            }
        }

        public float Y
        {
            get => vectorValue.Y;
            set
            {
                if(uniform)
                {
                    vectorValue.X += (value - vectorValue.Y) / Ratio;
                    OnPropertyChanged(nameof(X));
                }
                vectorValue.Y = value;
              //  OnPropertyChanged(nameof(Y));
            }
        }

        public VectorNodeProperty(Node nodeHost) : base(nodeHost)
        {
        }

        public VectorNodeProperty(Node nodeHost, string description, string name)
            : base(nodeHost, description, name)
        {
        }

        protected override void NodePropertyBase_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.NodePropertyBase_PropertyChanged(sender, e);
            if(e.PropertyName.Equals(nameof(X)) || e.PropertyName.Equals(nameof(Y)))
            {
                nodeHost.GetTimeline().CacheManager.ClearAllFramesAfter(this, 0);
            }
        }

        public override void CreateAnimationNode()
        {
            if (this.FindAnimationNode() == null)
            {
                //this is not the right way to do this if you add a normal node use TryAttachNode
                ForceAttachHiddenNode(new AnimationVectorPropertyNode());
            }
        }

        /// <summary>
        /// this will get called when the user has click on the property value 
        /// and wants to set it. He might have typed an expression or something
        /// returns the error if there is one
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public Exception SetXFromUserInput(string value)
        {
            return SetFromUserInput(value, m => m.X, d => X = d);
        }

        public Exception SetYFromUserInput(string value)
        {
            return SetFromUserInput(value, m => m.Y, d => Y = d);
        }

        public Exception SetFromUserInput(string value, Func<Vector2, float> getProperty, Action<float> setProperty)
        {
            object cachedVal =
                GetCache(nodeHost.GetTimeline().CurrentFrame, hiddenNodes.Count - 1).GetChannelData<Vector2>(Node.PROPERTY_OUT_CHANNEL);
            if (cachedVal == null)
                return new Exception("cached in value not available");
            
            if (pythonScope == null)
                pythonScope = Python.Engine.CreateScope();
            pythonScope.SetVariable("value", getProperty((Vector2)cachedVal));

            try
            {
                dynamic res = Python.Engine.Execute(value, pythonScope);
                setProperty(ToolBox.ConvertToFloat(res));
            }
            catch (Exception e)
            {
                return e;
            }

            return null;
        }
    }
}
