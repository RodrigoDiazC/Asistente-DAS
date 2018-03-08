using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Asistente_DAS
{
    /// <summary>
    /// Interaction logic for actividadesControl.xaml
    /// </summary>
    public partial class actividadesControl : UserControl
    {
        public DateTime fechaStart = DateTime.Now;//DateTime.Now;
        public DateTime fechaEnd = DateTime.Now; // Este lo pudieras reemplazar por el tiempo del siguiente actividad

        private Boolean isPrimary = true;
        private Boolean isSelected = false;

        //---- Constructor
        public actividadesControl()
        {
            InitializeComponent();
            Storyboard storyBoard = (Storyboard)this.FindResource("an_New");
            storyBoard.Begin();
        }

        //---- Constructor para una actividad restaurada de archivo
        public actividadesControl(DateTime date, string act, string proy, string obs)
        {
            InitializeComponent();

            fechaStart = date;
            textBox_Actividad.Text = act;
            textBox_Proyecto.Text = proy;
            textBox_Observaciones.Text = obs;

            userControl_ToSecondary();

            isSelected = false;
            Storyboard storyBoard = (Storyboard)this.FindResource("an_nonSelect");
            storyBoard.Begin();
        }

        //---- Devuelve la actividad
        public ActividadesSheet getActivity()
        {
            return new ActividadesSheet { Proyecto = textBox_Proyecto.Text, Actividad = textBox_Actividad.Text, Inicio = fechaStart, Observaciones = textBox_Observaciones.Text };
        }

        //---- Minimiza el control y detiene el cronometro
        public void userControl_ToSecondary()
        {
            Storyboard storyBoard = (Storyboard)this.FindResource("an_Secondary");
            storyBoard.Begin();

            fechaEnd = DateTime.Now;

            isPrimary = false;
        }

        //---- Maximiza el control
        public void userControl_ToPrimary()
        {
            isPrimary = true;
            Storyboard storyBoard = (Storyboard)this.FindResource("an_New");
            storyBoard.Begin();
        }

        //---- Expande el control para modificar las Observaciones
        private void userControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!isSelected)
            {
                Storyboard storyBoard = (Storyboard)this.FindResource("an_Select");
                storyBoard.Begin();
            }
            else
            {
                Storyboard storyBoard = (Storyboard)this.FindResource("an_nonSelect");
                storyBoard.Begin();
            }

            isSelected = !isSelected;
        }

        //---- Animación cuando el puntero sale del control
        private void userControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isPrimary)
            {
                //Storyboard storyBoard = (Storyboard)this.FindResource("an_Leave");
                //storyBoard.Begin();
            }
            if (!isPrimary)
            {
                Storyboard storyBoard = (Storyboard)this.FindResource("an_Secondary");
                storyBoard.Begin();
            }
        }

        //---- Animación cuando el puntero entra en el control
        private void userControl_MouseEnter(object sender, MouseEventArgs e)
        {
            Storyboard storyBoard = (Storyboard)this.FindResource("an_Enter");
            storyBoard.Begin();

            if (isPrimary)
            {
                //---- Agrega el tooltip que muestra la hora de inicio y el tiempo que lleva
                ToolTip ToolTip1 = new ToolTip { Content = "Hora de inicio: " + fechaStart.ToShortTimeString() + "\n" + "Tiempo transcurrido: " + (int)DateTime.Now.Subtract(fechaStart).TotalMinutes / 60 + " Horas " + (int)DateTime.Now.Subtract(fechaStart).TotalMinutes % 60 + " Minutos " };
                this.ToolTip = ToolTip1;
            }
            else
            {
                //---- Agrega el tooltip que muestra la hora de inicio y el tiempo que lleva
                ToolTip ToolTip1 = new ToolTip { Content = "Hora de inicio: " + fechaStart.ToShortTimeString() + "\n" + "Tiempo transcurrido: " + (int)fechaEnd.Subtract(fechaStart).TotalMinutes / 60 + " Horas " + (int)fechaEnd.Subtract(fechaStart).TotalMinutes % 60 + " Minutos " };
                this.ToolTip = ToolTip1;
            }
        }

        //---- Elimina el control
        private void button_Eliminar_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Window.GetWindow(this);

            //---- Se remueve a si mismo de su contenedor
            ((StackPanel)this.Parent).Children.Remove(this);

            //---- Convierte en primario el control anterior
            parentWindow.onDeletedActivity(isPrimary ? true : false);
        }

        private void button_Tiempo_Click(object sender, RoutedEventArgs e)
        {
            if (!isSelected)
            {
                Storyboard storyBoard = (Storyboard)this.FindResource("an_Select");
                storyBoard.Begin();
                isSelected = true;
            }

            //----Da el tiempo a los controles
            switch (fechaStart.DayOfWeek)
            {
                case DayOfWeek.Sunday:      ComboBox_Dia.SelectedIndex = 0; break;
                case DayOfWeek.Monday:      ComboBox_Dia.SelectedIndex = 1; break;
                case DayOfWeek.Tuesday:     ComboBox_Dia.SelectedIndex = 2; break;
                case DayOfWeek.Wednesday:   ComboBox_Dia.SelectedIndex = 3; break;
                case DayOfWeek.Thursday:    ComboBox_Dia.SelectedIndex = 4; break;
                case DayOfWeek.Friday:      ComboBox_Dia.SelectedIndex = 5; break;
                case DayOfWeek.Saturday:    ComboBox_Dia.SelectedIndex = 6; break;
            }

            TextBox_Hora.Text    = fechaStart.Hour.ToString();
            TextBox_Minutos.Text = fechaStart.Minute.ToString();

            Grid_Tiempo.Visibility = Visibility.Visible;
        }

        private void Button_CambiarTiempo_Click(object sender, RoutedEventArgs e)
        {
            DayOfWeek dayT = (DayOfWeek)(ComboBox_Dia.SelectedIndex);
            int dayF = 0;
              
            //---- Busca el numero del dia
            dayF = (int)fechaStart.DayOfWeek - (int)dayT;
            dayF = (int)fechaStart.Day - dayF;

            fechaStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, dayF, Convert.ToInt32(TextBox_Hora.Text), Convert.ToInt32(TextBox_Minutos.Text),0);
       
            Grid_Tiempo.Visibility = Visibility.Hidden;

            //---- Reordena la lista
            MainWindow parentWindow = (MainWindow)Window.GetWindow(this);
            parentWindow.saveList();
            parentWindow.displaySavedActivities();

        }
    }
}