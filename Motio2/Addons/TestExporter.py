from Motio.UI.Utils.Export import TimelineExporter as BaseClass
from Motio.Configuration import ConfigEntry
from System import Boolean, String, Int32, Enum

class TestExporter(BaseClass):
    classNameStatic = "Test"

    def BeforeExport(self):
        pass

    def ExportCurrentFrame(self, path):
        print "exporting to ", path, self.Options["name1"].Value

    def AfterExport(self):
        pass

    def Filter(self):
        return "SVG File (*.svg)|*.svg"

    def MakeOptions(self):
        entry1 = ConfigEntry[Boolean]()
        entry1.ShortName = "name1"
        entry1.LongName = "description1"
        entry1.Value = True
        
        entry2 = ConfigEntry[String]()
        entry2.ShortName = "file prop"
        entry2.LongName = "description2"
        entry2.Value = "default.file"

        entry3 = ConfigEntry[Int32]()
        entry3.ShortName = "int prop"
        entry3.LongName = "description3"
        entry3.Value = 15

        return [entry1, entry2, entry3]
