from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
import Motio.NodeImpl.NodePropertyTypes as Props
import System.Collections as Collections
from Motio.Meshing import Mesh, MeshGroup, Vertex
from Motio.Geometry import Vector2, Vector4
from examples.TestPyNodeTool import TestPyNodeTool

class TestObj(object):
    @staticmethod
    def CreateLoadInstance(parent, type):
        #set real values base on parent/type if necessary, in this example 
        #this method is not necessary
        print "create load instance in the secondary object"
        return TestObj()
    
    def __init__(self):
        #set default values in init
        self.valval = "lll"
        self.val = 668.24

    #on "custom objects" in python CustomSaver and CustomLoader are necessary 
    #otherwise the attribute will not be saved properly in json (try it without and checkout the json)
    def CustomSaver(self):
        #this is called when saving and shoudl return a dictionnary of <string,object>
        print "custom saver called on secondary object"
        d = Collections.Hashtable()
        d.Add("valval", self.valval)
        d.Add("val", self.val)
        return d;
    
    def CustomLoader(self, jobj):
        print "call load obj on secondary object"
        self.valval = jobj["valval"]
        self.val = jobj["val"]
        print "custom load obj: " + str(self.valval) + ";"  + str(self.val)

class TestPyGraphics(BaseClass):
    #this is necessary and is the named displayed in the dropdown list
    classNameStatic = "TePyGrapics"
    #the attribute in the following list will be saved to json
    saveAttrs = ["myprop"]
    
    #loading/saving callbacks
    
    #the CreateLoadInstance is not necessary here but can be useful
    #the function is supposed to return an instance of the type it's defined in
    #so here it should return and instance of "TestPyGraphics"
    #it is called and given the instance of the object that will contain the created instance
    #so you can create the instance based on that
    #note that their is no guarranty that the properties/fields of the parent instance are all 
    #valid, it depends on the structure of the json file
    #@staticmethod
    #def CreateLoadInstance(parent, type):
    #    print "create load instance"
    #    return TestPyGraphics()

    @staticmethod
    def CreateLoadInstance(parent, type):
        print "create load instance"
        instance = TestPyGraphics()
        instance.nodeHost = parent
        return instance
    
    def OnDoneLoading(self):
        print "on done loading"

    def OnAllLoaded(self, jobj):
        print "all the elements are loaded"
        
    #this is necessary to call the BaseClass's constructor, removing this will break all 
    #the things (or just make it so your node doesn't work)
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    #this can be concidered as the "constructor" of the node, it's called just after the object instance creation
    #use this instead of __init__
    def setup_node(self):
        print "setup node"
        #you need to set all the saved attribute in SetupNode to at least a dummy value of type it's going to be, not None
        #otherwise the CreateLoadInstance method can't be found
        self.myprop = TestObj()
        print "adding tool"
        self.Tools.Add(TestPyNodeTool(self))

    def evaluate_frame(self, frame, dataFeed):
        size = self.Properties.GetValue("test_double", frame)
        vertices = [
            Vertex(Vector2(size, size),
                   Vector4(1, 0, 0, 1),
                   Vector2.Normalize(Vector2.One),
                   Vector2(size, size)),
            Vertex(Vector2(-size, size),
                   Vector4(0, 1, 0, 1)),
            Vertex(Vector2(-size, -size),
                   Vector4(0, 0, 1, 1)),
            Vertex(Vector2(size, -size),
                   Vector4(0, 0, 0, 0))
        ]
        #modifying a vertex list
        for i in range(len(vertices)):
            #I created helper method to set pos/color/normal/uv 
            #because iron python doesn't let you modify a struct from python
            #so doing vertex.position = ... doesnt work but
            # doing vertex.SetPos(...) does
            vertices[i].SetPos(vertices[i].position + vertices[i].normal*3)

        triangles = [
            0,1,2,
            0,2,3
        ]
        mesh = Mesh()
        mesh.triangles = triangles
        mesh.vertices = vertices
        dataFeed.SetChannelData("mesh", MeshGroup(mesh))

    def setup_properties(self):
        self.Properties.Add("test_double", Props.FloatNodeProperty(self, "test double", "ChikaTozor"), 3)
        self.Properties.Add("test_dropdown", 
            Props.DropdownNodeProperty(self, "test dropdown", "Zozotozor",
                ["ppp", "pp", "p"]), "ppp")
        self.Properties.Add("test_text", Props.StringNodeProperty(self, "Test string", "Patatozor"), "default string value")
        