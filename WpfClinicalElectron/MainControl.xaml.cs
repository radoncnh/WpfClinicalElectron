using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace WpfClinicalElectron
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        public MainControl()
        {
            InitializeComponent();
        }

        public Patient patient;

        private void calculateButton_Click(object sender, RoutedEventArgs e)
        {
            string phantom_selection = ((ComboBoxItem)phantomComboBox.SelectedItem).Content.ToString();
            string circle_diameter = circleTextBox.Text;
            string selected_machine = ((ComboBoxItem)machineComboBox.SelectedItem).Content.ToString();
            string selected_energy = ((ComboBoxItem)energyComboBox.SelectedItem).Content.ToString();
            string selected_gantry_angle = gantryangleTextBox.Text;
            string selected_collimator_angle = collimatorangleTextBox.Text;
            string selected_couch_angle = couchangleTextBox.Text;
            string selected_SSD = ssdTextBox.Text;
            string selected_dose = doseTextBox.Text;
            string selected_fractions = fractionsTextBox.Text;

            double gantryAngle = 0.0;
            bool canConvert = double.TryParse(selected_gantry_angle, out gantryAngle);
            if (canConvert == false)
            { 
                if (selected_machine != "StJ_TB_3190")
                    gantryAngle = 180.0;
                else
                    gantryAngle = 0.0;
            }

            double collimatorAngle = 0.0;
            canConvert = double.TryParse(selected_collimator_angle, out collimatorAngle);
            if (canConvert == false)
            {
                if (selected_machine != "StJ_TB_3190")
                    collimatorAngle = 180.0;
                else
                    collimatorAngle = 0.0;
            }

            double couchAngle = 0.0;
            canConvert = double.TryParse(selected_couch_angle, out couchAngle);
            if (canConvert == false)
            {
                if (selected_machine != "StJ_TB_3190")
                    couchAngle = 180.0;
                else
                    couchAngle = 0.0;
            }

            if (selected_machine != "StJ_TB_3190")
            {
                if (gantryAngle < 0.0 || gantryAngle >= 360.0)
                    gantryAngle = 180.0;
                if (collimatorAngle < 0.0 || collimatorAngle >= 360.0)
                    collimatorAngle = 180.0;
                if (couchAngle < 0.0 || couchAngle >=360.0)
                    couchAngle = 180.0;
            }
            else
            {
                if (gantryAngle < 0.0 || gantryAngle >= 360.0)
                    gantryAngle = 0.0;
                if (collimatorAngle < 0.0 || collimatorAngle >= 360.0)
                    collimatorAngle = 0.0;
                if (couchAngle < 0.0 || couchAngle >= 360.0)
                    couchAngle = 0.0;
            }

            int phantom_slice_num;
            int fractions;

            double phantom_radius_mm = 150.0;
            double phantom_diameter_cm = 30.0;

            double dose_per_fx;

            if (!Double.TryParse(selected_dose, out dose_per_fx))
            {
                dose_per_fx = 180.0;
            }

            if (!int.TryParse(selected_fractions, out fractions))
            {
                fractions = 1;
            }

            if (!Double.TryParse(circle_diameter, out phantom_diameter_cm))
            {
                phantom_diameter_cm = 30.0;
            }


            if (phantom_diameter_cm < 10.0)
                phantom_diameter_cm = 10.0;
            if (phantom_diameter_cm > 50.0)
                phantom_diameter_cm = 50.0;

            phantom_radius_mm = (phantom_diameter_cm / 2.0) * 10.0;

            phantom_slice_num = Convert.ToInt32((phantom_diameter_cm * 10.0) / 2.5);
            phantom_slice_num += 1;  // Force phantom_slice_num to be odd


            StructureSet ph = patient.AddEmptyPhantom("ElectronPhantom", PatientOrientation.HeadFirstSupine, 512, 512, 600, 600, phantom_slice_num, 2.5);

            Structure body = ph.AddStructure("EXTERNAL", "BODY");

            if (phantom_selection == "Spherical")
            {   // sphere creation
                int center_slice = (phantom_slice_num - 1) / 2;
                int slice_mirror = center_slice - 1;
                double radius = phantom_radius_mm;
                double x_pt;

                for (int i = center_slice; i < phantom_slice_num; i++)
                {
                    x_pt = 2.5 * (i - center_slice);
                    if (x_pt < phantom_radius_mm)
                    {
                        radius = Math.Sqrt((phantom_radius_mm * phantom_radius_mm) - (x_pt * x_pt));
                        if (i == center_slice)
                        {
                            draw_circle_on_plane(body, i, radius);
                        }
                        else
                        {
                            draw_circle_on_plane(body, i, radius);
                            draw_circle_on_plane(body, slice_mirror, radius);
                            slice_mirror -= 1;
                        }
                    }
                }
            }

            if (phantom_selection == "Cylindrical")
            {   // cylinder creation
                int center_slice = (phantom_slice_num - 1) / 2;
                int slice_mirror = center_slice - 1;
                double radius = phantom_radius_mm;
                double x_pt;

                for (int i = center_slice; i < phantom_slice_num; i++)
                {
                    x_pt = 2.5 * (i - center_slice);
                    if (x_pt < phantom_radius_mm)
                    {
                        if (i == center_slice)
                        {
                            draw_circle_on_plane(body, i, radius);
                        }
                        else
                        {
                            draw_circle_on_plane(body, i, radius);
                            draw_circle_on_plane(body, slice_mirror, radius);
                            slice_mirror -= 1;
                        }
                    }
                }
            }

            Course course = patient.AddCourse();
            course.Id = "Clinical Setup";

            ExternalPlanSetup electron_plan = course.AddExternalPlanSetup(ph);
            electron_plan.SetPrescription(fractions, new DoseValue(dose_per_fx, DoseValue.DoseUnit.cGy), 1.0);

            ExternalBeamMachineParameters machine_parameters = new ExternalBeamMachineParameters(selected_machine, selected_energy, 600, "STATIC", null);
            VRect<double> jawPositions = new VRect<double>(-100, -100, 100, 100);

            if (selected_machine != "StJ_TB_3190")
            { 
                gantryAngle = varian_angle_to_iec_angle(gantryAngle);
                collimatorAngle = varian_angle_to_iec_angle(collimatorAngle);
                couchAngle = varian_angle_to_iec_angle(couchAngle);
            }

            VVector iso_body = body.CenterPoint;
            VVector isocenter = new VVector(iso_body.x, iso_body.y, iso_body.z);
            ph.Image.UserOrigin = isocenter;

            double isocenter_radius_mm;
            double SSD = 100.0;

            SSD = Convert.ToDouble(selected_SSD);
            if (SSD < 90.0)
                SSD = 90.0;
            if (SSD > 110.0)
                SSD = 110.0;

            isocenter_radius_mm = phantom_radius_mm + ((SSD - 100.0)) * 10.0;
            isocenter.x = isocenter_radius_mm * Math.Sin(degreesToradians(gantryAngle));
            isocenter.y = (isocenter_radius_mm * Math.Cos(degreesToradians(gantryAngle)))*-1.0;

            Beam beam = electron_plan.AddStaticBeam(machine_parameters, jawPositions, collimatorAngle, gantryAngle, couchAngle, isocenter);
            
            MessageBox.Show("Finished! ");
        }

        double varian_angle_to_iec_angle(double angle_varian)
        {
            double angle_temp, angle_iec;

            angle_temp = angle_varian - 180.0;

            if (angle_temp <= 0.0)
                angle_iec = Math.Abs(angle_temp);
            else
                angle_iec = 540.0 - angle_varian;

            return angle_iec;
        }

        double degreesToradians(double degrees)
        {
            double radians;

            radians = (Math.PI * degrees) / 180.0;

            return radians;
        }

        void draw_circle_on_plane(Structure Body, int slice, double circle_radius_mm)
        {
            int num_theta = 50;
            double x, y, theta = 0.0;
            double dtheta = 2.0 * Math.PI / (double)num_theta;
            VVector[] circle_coords = new VVector[num_theta];

            for (int i = 0; i < num_theta; i++)
            {
                x = circle_radius_mm * Math.Cos(theta);
                y = circle_radius_mm * Math.Sin(theta);

                //VVectors to add distances are in mm

                circle_coords[i] = new VVector(x, y, slice);
                theta += dtheta;
            }

            Body.AddContourOnImagePlane(circle_coords, slice);
            Body.SetAssignedHU(0.0);
        }
    }
}
