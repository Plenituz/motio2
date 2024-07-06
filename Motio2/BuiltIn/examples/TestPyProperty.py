from Motio.NodeImpl.PropertyAffectingNodes import PyPropertyAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import FloatNodeProperty
from System import Single
from Motio.NodeCore import Node

class TestPyProperty(BaseClass):
    #name displayed in the node list 
    classNameStatic = "testPyProperty"
    #here you should return a list of what type of Properties 
    #this node can be added on
    #the types should be .NET type object as the example shows
    acceptedPropertyTypes = [Single.__clrtype__()]

    #should return the name displayed in the property panel, usualy the same as the 
    #name displayed in the node list
    def get_class_name(self):
        return TestPyProperty.classNameStatic

    #to add any property to the node you should do it in this function
    #you don't have to add properties to your node but you have to 
    #implement this function, even if you leave it empty
    #it's this way so people are not tempted to add properties in the SetupNode function
    #which would cause problemes when creating a node from a json file
    def setup_properties(self):
        print "adding all kind of properties to this node"
        self.Properties.Add("testprop", FloatNodeProperty(self, "test property", "Test"), 3)

    #use this function as a node constructor, this is optionnal
    def setup_node(self):
        print "node constructor"

    #this is the core of the node, this function is called for every frame 
    #this is where you modify the property value 
    def evaluate_frame(self, frame, dataFeed):
        propVal = self.Properties.GetValue("testprop", frame)
        print propVal
        previousVal = 0
        #get the previous val if there is one
        if dataFeed.ChannelExists("propertyOut"):
            previousVal = dataFeed.GetChannelData(Node.PROPERTY_OUT_CHANNEL);

        dataFeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, previousVal + propVal)