using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Threading;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml.Serialization;

namespace Asistente_DAS
{
    //---- Funciones para efecto BLUR
    internal enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_INVALID_STATE = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    internal enum WindowCompositionAttribute
    {
        // ...
        WCA_ACCENT_POLICY = 19

        // ...
    }

    //---- Clase para guardar las actividades
    public class ActividadesSheet
    {
        public string Proyecto { get; set; }
        public string Actividad { get; set; }
        public string Observaciones { get; set; }
        public DateTime Inicio { get; set; }
    }

    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //---- Efecto BLUR
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        //---- Lista de actividades
        private List<ActividadesSheet> listaActividades = new List<ActividadesSheet>();

        //---- Preferencias
        private string nombre_Usuario = "Nombre de Usuario";
        private string nombre_Gerente = "Nombre de Gerente";
        private int notificaciones_modo = 0;
        private bool recordatorio_activo = false;

        //---- Timer para notificaciones
        DispatcherTimer timer_Notificaciones = new DispatcherTimer();
        DispatcherTimer timer_Recordatorio = new DispatcherTimer();

        //----------------------------------------------------------------------------------------------

        //---- Constructor
        public MainWindow()
        {
            InitializeComponent();

            //-------------------------------------------------------- Carga configuraciones
            loadPreferences();

            //--------------------------------------------------------- Lista de actividades
            listaActividades = loadSavedActivities();
            if (listaActividades != null && listaActividades.Count > 0) displaySavedActivities();
            else listaActividades = new List<ActividadesSheet>();

            //--------------------------------------------------------- Fecha de TOP
            DateTime temp = DateTime.Now;
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            System.Globalization.Calendar cal = dfi.Calendar;
            label_Semana.Content = "Semana " + cal.GetWeekOfYear(temp, dfi.CalendarWeekRule, dfi.FirstDayOfWeek) + ", " + cal.GetYear(temp) + " " + getNameInitials(nombre_Usuario);

            //-------------------------------------------------------- Inicializa timer
            timersSettup();

        }

        //---- Habilita el efecto BLUR
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnableBlur();
        }

        //---- Habilita poder mover la ventana
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        //-------------------------------------------------------------------------- Botones

        //---- Nueva actividad TODO Cambiar nombre acorde al nombre del boton y considerar moverlo a Codigo.cs
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //---- Añade la etiqueta de "Hoy Día, Semana"
            if (grid_Actividades.Children.Count == 0)
                grid_Actividades.Children.Insert(0, addLabelToContainer(DateTime.Now.ToLongDateString()));
            else if (grid_Actividades.Children.Count > 2 && grid_Actividades.Children[1] is actividadesControl)
                if ((grid_Actividades.Children[1] as actividadesControl).fechaStart.Date != DateTime.Now.Date)
                    grid_Actividades.Children.Insert(0, addLabelToContainer(DateTime.Now.ToLongDateString()));

            //---- Minimza la actividad anterior
            if (grid_Actividades.Children.Count > 1 && grid_Actividades.Children[1] is actividadesControl)
                (grid_Actividades.Children[1] as actividadesControl).userControl_ToSecondary();

            //---- Crea la nueva actividad
            actividadesControl act = new actividadesControl();
            act.HorizontalAlignment = HorizontalAlignment.Left;
            act.Margin = new Thickness(5, 10, 0, 8);

            //---- Mismo proyecto que la actividad anterior
            if (grid_Actividades.Children.Count > 1 && grid_Actividades.Children[1] is actividadesControl)
                act.textBox_Proyecto.Text = (grid_Actividades.Children[1] as actividadesControl).textBox_Proyecto.Text;

            //---- Inserta el nuevo control (Actividad)
            grid_Actividades.Children.Insert(1, act);
        }

        //---- Genera archivo TODO Cambiar nombre acorde al nombre del boton y considerar moverlo a Codigo.cs
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            generateExcel();
        }

        //---- Aplica configuraciones
        private void button_ConfAplicar_Click(object sender, RoutedEventArgs e)
        {
            if (textBox_NombreUsuario.Text.Length < 1 && textBox_NombreUsuario.Text.Length < 1) return;

            nombre_Usuario = textBox_NombreUsuario.Text;
            nombre_Gerente = textBox_NombreGerente.Text;
            notificaciones_modo = comboBox_Notificaciones.SelectedIndex;
            recordatorio_activo = (bool)checkBox_Recordatorio.IsChecked;

            scroll_actividades.Visibility = Visibility.Visible;
            scroll_Configuracion.Visibility = button_ConfAplicar.Visibility = Visibility.Collapsed;

            //--------------------------------------------------------- Fecha de TOP
            DateTime temp = DateTime.Now;
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            System.Globalization.Calendar cal = dfi.Calendar;
            label_Semana.Content = "Semana " + cal.GetWeekOfYear(temp, dfi.CalendarWeekRule, dfi.FirstDayOfWeek) + ", " + cal.GetYear(temp) + " " + getNameInitials(nombre_Usuario);

            //---- Inicia los timers
            timersSettup();

            savePreferences();
        }

        //---- Resetea las configuraciones
        private void button_ResetApp_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Si resetea la aplicación se borrarán sus preferencias y actividades.\n¿Desa resetear de todos modos?", "Resetear aplicación", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Properties.Settings.Default.Reset();
                Properties.Settings.Default.Save();

                if (File.Exists("savedActivities.xml"))
                    File.Delete("savedActivities.xml");

                listaActividades.Clear();
                grid_Actividades.Children.Clear();

                Process.Start("AsistenteDAS.exe");
                Application.Current.Shutdown();
            }
        }

        //---------------------------------------------------------------------- Animaciones, vistas, efectos...
        //---- Efecto BLUR
        internal void EnableBlur()
        {
            var windowHelper = new WindowInteropHelper(this);

            var accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;

            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        // Visibilidad del boton de configuraciones
        private void grid_subTop_MouseEnter(object sender, MouseEventArgs e)
        {
            button_Conf.Visibility = Visibility.Visible;
        }

        private void grid_subTop_MouseLeave(object sender, MouseEventArgs e)
        {
            button_Conf.Visibility = Visibility.Hidden;
        }

        private void button_Conf_Click(object sender, RoutedEventArgs e)
        {
            textBox_NombreUsuario.Text = nombre_Usuario;
            textBox_NombreGerente.Text = nombre_Gerente;
            comboBox_Notificaciones.SelectedIndex = notificaciones_modo;
            checkBox_Recordatorio.IsChecked = recordatorio_activo;

            scroll_actividades.Visibility = scroll_actividades.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            scroll_Configuracion.Visibility = scroll_Configuracion.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            button_ConfAplicar.Visibility = button_ConfAplicar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void button_ConfSiguiente_Click(object sender, RoutedEventArgs e)
        {
            grid_Bienvenida.Visibility = Visibility.Collapsed;
            scroll_Configuracion.Visibility = button_ConfAplicar.Visibility = Visibility.Visible;
        }
    }
}
