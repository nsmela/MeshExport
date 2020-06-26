using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;

namespace MeshExport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        VMS.TPS.Common.Model.API.Application app;
        string patient_name;
        string patient_id;
        List<MeshInfo> _meshes;

        struct MeshInfo
        {
            public string id;
            public string structureset;
            public MeshGeometry3D mesh;
            public string last_edit;
            public double volume;
        }

        public MainWindow()
        {
            InitializeComponent();
            app = ESAPIApplication.Instance.Context;
            textblockPatientInfo.Text = "No Patient Selected";
        }

        //used to cleanly close the program
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ESAPIApplication.Dispose();
        }

        //ESAPI Calls
        /// <summary>
        /// Contacts Eclipse directly for each structure's info instead of getting the patient info
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private List<MeshInfo> GetMeshFromPatient(string id)
        {
            List<MeshInfo> meshes = new List<MeshInfo>();
            try
            {
                app.ClosePatient();
            }
            catch { }

            try
            {
                Patient patient = app.OpenPatientById(id);
                patient_name = patient.LastName + ", " + patient.FirstName;
                patient_id = patient.Id.ToString();
                foreach (StructureSet s in patient.StructureSets)
                    foreach(Structure ss in s.Structures)
                    {
                        if (!ss.HasSegment || ss.Volume == 0)
                            continue;

                        var mesh = new MeshInfo();
                        mesh.id = ss.Id;
                        mesh.structureset = s.Id;
                        mesh.last_edit = ss.HistoryDateTime.ToLongDateString();
                        mesh.volume = ss.Volume;
                        mesh.mesh = ss.MeshGeometry;
                        meshes.Add(mesh);
                    }
            }
            catch { }

            return meshes;
        }

        //ESAPI Processing
        private string GetPatientInfo(Patient patient)
        {
            if (patient == null)
                return "Invalid Patient!";

            string text = "Patient Info:\r\n";

            text += "NAME: " + patient.LastName.ToUpper() + ", " + patient.FirstName.ToUpper();

            return text;
        }   

        /// <summary>
        /// This method saves the given MeshGeometry3D to the STL file format, including surface normals
        /// The file should be ready for 3D printing
        /// </summary>
        /// <param name="mesh">Trianglemesh to export</param>
        /// <param name="outputFileName">File path and name of the file to write.</param>
        void SaveTriangleMeshtoStlFile(MeshGeometry3D mesh, string outputFileName)
        {
            if (mesh == null)
                return;

            if (File.Exists(outputFileName))
            {
                File.SetAttributes(outputFileName, FileAttributes.Normal);
                File.Delete(outputFileName);
            }

            Point3DCollection vertexes = mesh.Positions;
            Int32Collection indexes = mesh.TriangleIndices;

            Point3D p1, p2, p3;
            Vector3D n;

            string text;

            using (TextWriter writer = new StreamWriter(outputFileName))
            {
                writer.WriteLine("solid Bolus");

                for (int v = 0; v < mesh.TriangleIndices.Count(); v += 3)
                {
                    //gather the 3 points for the face and the normal
                    p1 = vertexes[indexes[v]];
                    p2 = vertexes[indexes[v + 1]];
                    p3 = vertexes[indexes[v + 2]];
                    n = CalculateSurfaceNormal(p1, p2, p3);

                    text = string.Format("facet normal {0} {1} {2}", n.X, n.Y, n.Z);
                    writer.WriteLine(text);
                    writer.WriteLine("outer loop");
                    text = String.Format("vertex {0} {1} {2}", p1.X, p1.Y, p1.Z);
                    writer.WriteLine(text);
                    text = String.Format("vertex {0} {1} {2}", p2.X, p2.Y, p2.Z);
                    writer.WriteLine(text);
                    text = String.Format("vertex {0} {1} {2}", p3.X, p3.Y, p3.Z);
                    writer.WriteLine(text);
                    writer.WriteLine("endloop");
                    writer.WriteLine("endfacet");

                }


            }
        }

        /// <summary>
        /// creates a faces surface normal from the faces three points
        /// </summary>
        Vector3D CalculateSurfaceNormal(Point3D p1, Point3D p2, Point3D p3)
        {
            Vector3D v1 = new Vector3D(0, 0, 0);             // Vector 1 (x,y,z) & Vector 2 (x,y,z)
            Vector3D v2 = new Vector3D(0, 0, 0);
            Vector3D normal = new Vector3D(0, 0, 0);

            // Finds The Vector Between 2 Points By Subtracting
            // The x,y,z Coordinates From One Point To Another.

            // Calculate The Vector From Point 2 To Point 1
            v1.X = p1.X - p2.X;                  // Vector 1.x=Vertex[0].x-Vertex[1].x
            v1.Y = p1.Y - p2.Y;                  // Vector 1.y=Vertex[0].y-Vertex[1].y
            v1.Z = p1.Z - p2.Z;                  // Vector 1.z=Vertex[0].y-Vertex[1].z
                                                 // Calculate The Vector From Point 3 To Point 2
            v2.X = p2.X - p3.X;                  // Vector 1.x=Vertex[0].x-Vertex[1].x
            v2.Y = p2.Y - p3.Y;                  // Vector 1.y=Vertex[0].y-Vertex[1].y
            v2.Z = p2.Z - p3.Z;                  // Vector 1.z=Vertex[0].y-Vertex[1].z

            // Compute The Cross Product To Give Us A Surface Normal
            normal.X = v1.Y * v2.Z - v1.Z * v2.Y;   // Cross Product For Y - Z
            normal.Y = v1.Z * v2.X - v1.X * v2.Z;   // Cross Product For X - Z
            normal.Z = v1.X * v2.Y - v1.Y * v2.X;   // Cross Product For X - Y

            normal.Normalize();

            return normal;
        }

        //User Control Commands

        private void Patient_Keydown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Search_Click(null, e);
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string id = textSearch.Text;

            if (id.Length < 1)
                return;

            //initialize the window user controls
            listboxStructures.ItemsSource = null;
            textblockStructureInfo.Text = "Structure Info:";
            buttonExportMesh.Visibility = Visibility.Collapsed;

            _meshes = GetMeshFromPatient(id);

            foreach (MeshInfo m in _meshes)
            {
                string text = String.Format("({0}) - {1}", m.structureset, m.id);
                listboxStructures.Items.Add(text);
            }

            //parse info into the window
            textblockPatientInfo.Text = patient_name;
            
        }

        private void Structures_SelectionChanged(object sender, RoutedEventArgs e)
        {
            int listitem = listboxStructures.SelectedIndex;
            if (listitem < 0)
                return;

            //find the selected mesh from the list
            var mesh = _meshes[listitem];

            buttonExportMesh.Visibility = Visibility.Visible;

            string text = "Structure Info:\r\n";

            text += "Structure Set: " + mesh.structureset + "\r\n";
            text += "Volume: " + mesh.volume.ToString("0.00") + " mL\r\n";
            text += "Last Edit: " + mesh.last_edit + "\r\n";

            textblockStructureInfo.Text = text;

        }

        private void ExportMesh_Click(object sender, RoutedEventArgs e)
        {
            //ensure mesh is okay

            int listitem = listboxStructures.SelectedIndex;
            if (listitem < 0)
                return;

            var mesh = _meshes[listitem];

            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.Filter = "STL File|*.stl|All Files|*.*";
            sfd.Title = "Save Mesh File";
            sfd.FileName = patient_id + " - " + mesh.id;
            sfd.ShowDialog();

            if (sfd.FileName != "")
                SaveTriangleMeshtoStlFile(mesh.mesh, sfd.FileName);
        }

    }
}
