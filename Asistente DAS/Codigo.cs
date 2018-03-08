using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml.Serialization;

namespace Asistente_DAS
{
    public partial class MainWindow
    {
        //-------------------------------------------------------------------------- Métodos
        //---- Guarda las preferencias
        private void savePreferences()
        {
            Properties.Settings.Default["Usuario"] = nombre_Usuario;
            Properties.Settings.Default["Gerente"] = nombre_Gerente;
            Properties.Settings.Default["Notificaciones"] = comboBox_Notificaciones.SelectedIndex;
            Properties.Settings.Default["Recordatorio"] = (bool)checkBox_Recordatorio.IsChecked;
            Properties.Settings.Default["Configurado"] = true;

            Properties.Settings.Default.Save();
        }

        //---- Carga las preferencias
        private void loadPreferences()
        {
            Properties.Settings.Default.Reload();

            if (Properties.Settings.Default.Configurado)
            {
                nombre_Usuario = Properties.Settings.Default.Usuario;
                nombre_Gerente = Properties.Settings.Default.Gerente;
                notificaciones_modo = Properties.Settings.Default.Notificaciones;
                recordatorio_activo = Properties.Settings.Default.Recordatorio;

                scroll_actividades.Visibility = Visibility.Visible;
            }
            else
            {
                scroll_actividades.Visibility = scroll_Configuracion.Visibility = Visibility.Collapsed;
                grid_Bienvenida.Visibility = Visibility.Visible;
            }
        }

        //---- Guarda las actividades que esten en la lista
        public void saveList()
        {
            listaActividades.Clear();

            for (int i = 0; i < grid_Actividades.Children.Count; i++)
            {
                if (grid_Actividades.Children[i] is actividadesControl)
                {
                    listaActividades.Insert(0, (grid_Actividades.Children[i] as actividadesControl).getActivity());
                }
            }

            listaActividades = listaActividades.OrderBy(o => o.Inicio).ToList();
            saveCurrentListToFile(listaActividades);
        }

        //---- Esta función es llamada por el control cuando es eliminado
        public void onDeletedActivity(bool toPrimary)
        {
            //---- Vuelve a la actividad anterior en principal
            if (grid_Actividades.Children.Count > 1)
            {
                if (toPrimary && grid_Actividades.Children[1] is actividadesControl)
                    (grid_Actividades.Children[1] as actividadesControl).userControl_ToPrimary();
            }

            //---- Borra las etiquetas que no tengan actividades dentro de ese día
            for (int i = 0; i < grid_Actividades.Children.Count; i++)
            {
                if (grid_Actividades.Children.Count == 1) grid_Actividades.Children.RemoveAt(i);
                else if (grid_Actividades.Children[i] is Label)
                    if (grid_Actividades.Children[i + 1] is Label) grid_Actividades.Children.RemoveAt(i);
            }
        }

        //---- Genera archivo de Excel
        private void excelSaveProcess()
        {
            if (listaActividades.Count > 0)
            {
                //---- Infromación de fecha
                DateTime fechaActual = DateTime.Now;
                DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
                System.Globalization.Calendar cal = dfi.Calendar;
                double diferencia = 0;

                //---- Ruta del archivo plantilla
                string rutaTemplate = Directory.GetCurrentDirectory() + "\\Template.xls";
                string rutaFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\DAS";
                //--- Crea carpeta
                Directory.CreateDirectory(rutaFolder);

                //--- Nueva ruta del archivo
                string rutaNueva = rutaFolder + "\\DAS Semana " + cal.GetWeekOfYear(fechaActual, dfi.CalendarWeekRule, dfi.FirstDayOfWeek).ToString() + " " + fechaActual.ToString("yyyy") + " " + getNameInitials(nombre_Usuario) + ".xls";

                //--- Toolkit para Excel ----//
                Microsoft.Office.Interop.Excel.Workbook mWorkBook;
                Microsoft.Office.Interop.Excel.Sheets mWorkSheets;
                Microsoft.Office.Interop.Excel.Worksheet mWSheet1;
                Microsoft.Office.Interop.Excel.Application oXL;

                //--- Creando objeto y configurando parametros
                oXL = new Microsoft.Office.Interop.Excel.Application();
                oXL.Visible = false;    //Para que no abra la ventana de excel
                oXL.DisplayAlerts = false;
               

                //--- Abre el archivo
                mWorkBook = oXL.Workbooks.Open(rutaTemplate, 0, false, 5, "", "", false, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "", true, false, 0, true, false, false);

                //--- Get all the sheets in the workbook
                mWorkSheets = mWorkBook.Worksheets;

                //--- Get the allready exists sheet
                mWSheet1 = (Microsoft.Office.Interop.Excel.Worksheet)mWorkSheets.get_Item("Formato");    //Nombre de la hoja

                //------------------------------------------------------------------ Datos Esáticos
                //--- Nombres de usuario y gerente
                mWSheet1.Cells[5, 2] = nombre_Usuario;
                mWSheet1.Cells[5, 4] = nombre_Gerente;

                //--- Pone un "0" si es menor a 10
                int weekNumber = cal.GetWeekOfYear(fechaActual, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
                if (weekNumber < 10) mWSheet1.Cells[3, 1] = "Semana 0" + weekNumber + " 2017";
                else mWSheet1.Cells[3, 1] = "Semana " + weekNumber + " 2017";

                //--- Rango de días
                //--- Pone un "0" si es menor a 10
                string firstDay = listaActividades.First().Inicio.ToString("dd");
                string firstMonth = listaActividades.First().Inicio.ToString("MMM", new CultureInfo("es-ES")).ToUpper();
                string lastDay = listaActividades.Last().Inicio.ToString("dd");
                string lastMonth = listaActividades.Last().Inicio.ToString("MMM", new CultureInfo("es-ES")).ToUpper();

                mWSheet1.Cells[3, 2] = "DEL " + firstDay + "/" + firstMonth + " AL " + lastDay + "/" + lastMonth;

                //--- Guarda las actividades de la lista
                int i = 8;
                DateTime temp = DateTime.ParseExact("01/01/1970", "dd/MM/yyyy", CultureInfo.InvariantCulture);

                //---- Inserta las actividades a la hoja de excel. Las pone en orden inverso al de la lista ya que la lista se guarda al revés.
                for (int j = 0; j < listaActividades.Count; j++)
                {
                    //--- Inserta celda de fecha
                    if (listaActividades[j].Inicio.Day != temp.Day)
                    {
                        mWSheet1.Cells[i, 1] = listaActividades[j].Inicio.ToString("dddd dd", new CultureInfo("es-ES")).ToUpper();
                        temp = listaActividades[j].Inicio;

                        //---- Modifica la actividad para que la primera empieze a las 8 a.m. 
                        listaActividades[j].Inicio = new DateTime(listaActividades[j].Inicio.Year, listaActividades[j].Inicio.Month, listaActividades[j].Inicio.Day, 8, 0,0);
                    }

                    mWSheet1.Cells[i, 2] = listaActividades[j].Proyecto;
                    mWSheet1.Cells[i, 3] = listaActividades[j].Actividad;
                    mWSheet1.Cells[i, 4] = listaActividades[j].Observaciones;

                    //--- Calcula el tiempo de cada actividad
                    if ((j + 1) < listaActividades.Count)
                    {
                        if (listaActividades[j].Inicio.Day == listaActividades[j + 1].Inicio.Day)
                        {
                            diferencia = (listaActividades[j + 1].Inicio.Hour - listaActividades[j].Inicio.Hour);
                            //--- Quita una hora a la actividad dentro del horario de comida
                            if (listaActividades[j + 1].Inicio.Hour >= 14 && listaActividades[j].Inicio.Hour <= 13)
                                diferencia--;

                            diferencia += (Convert.ToDouble(listaActividades[j + 1].Inicio.Minute - listaActividades[j].Inicio.Minute) / 60d);
                        }
                        else
                        {
                            diferencia = (18 - listaActividades[j].Inicio.Hour);
                            diferencia += (Convert.ToDouble(0 - listaActividades[j].Inicio.Minute) / 60d);
                            if (listaActividades[j].Inicio.Hour <= 13)
                                diferencia--;
                        }
                    }
                    else
                    {
                        diferencia = (18 - listaActividades[j].Inicio.Hour);
                        diferencia += (Convert.ToDouble(0 - listaActividades[j].Inicio.Minute) / 60d);
                        if(listaActividades[j].Inicio.Hour <= 13)
                            diferencia--;
                    }

                    mWSheet1.Cells[i, 5] = Math.Abs(diferencia);

                    i++;
                }

                //--- Guarda el nuevo reporte
                try
                {
                    mWorkBook.SaveAs(rutaNueva, Microsoft.Office.Interop.Excel.XlFileFormat.xlWorkbookNormal,
                        Missing.Value, Missing.Value, Missing.Value, Missing.Value, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlExclusive,
                        Missing.Value, Missing.Value, Missing.Value,
                        Missing.Value, Missing.Value);

                    MessageBox.Show("Reporte generado exitosamente.\n " + rutaNueva);
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    MessageBox.Show("Por favor cierre el documento y vuelva a generar el reporte.\nError " + ex.Message.ToString());
                }

                mWorkBook.Close(Missing.Value, Missing.Value, Missing.Value);
                mWSheet1 = null;
                mWorkBook = null;
                oXL.Quit();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        //--- Abre carpeta donde se guarda el documento
        private void button_Carpeta_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\DAS");
        }

        //--- Guarda la lista
        private void button_Guardar_Click(object sender, RoutedEventArgs e)
        {
            saveList();
        }

        //--- Guarda la lista cuando se cierra la aplicación
        private void button_Cerrar_Click(object sender, RoutedEventArgs e)
        {
            saveList();
            timer_Notificaciones.Stop();
            timer_Recordatorio.Stop();
            Application.Current.Shutdown();
        }

        //--- Guarda la lista en un archivo
        public void saveCurrentListToFile(List<ActividadesSheet> listaAct)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<ActividadesSheet>));

            using (FileStream stream = File.Create("savedActivities.xml"))
            {
                serializer.Serialize(stream, listaAct);
            }
        }

        //--- Carga la lista desde un archivo
        public List<ActividadesSheet> loadSavedActivities()
        {
            if (!File.Exists("savedActivities.xml"))
            {
                MessageBox.Show("No se ha encontrado archivo de actividades.");
                return null;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(List<ActividadesSheet>));

            using (FileStream stream = File.OpenRead("savedActivities.xml"))
            {
                List<ActividadesSheet> dezerializedList = (List<ActividadesSheet>)serializer.Deserialize(stream);
                return dezerializedList;
            }
        }

        //--- Carga las actividades guardadas y las regresa como objeto
        private actividadesControl loadListToControl(List<ActividadesSheet> listaAct, int index)
        {
            actividadesControl act = new actividadesControl(listaAct[index].Inicio, listaAct[index].Actividad, listaAct[index].Proyecto, listaAct[index].Observaciones);
            return act;
        }

        //--- Añade etiquetas al contenedor de actividades
        private Label addLabelToContainer(string str)
        {
            Label lbl = new Label();
            BrushConverter bc = new BrushConverter();
            Brush brush = (Brush)bc.ConvertFrom("#FF1B7495");

            //---- Color del texto
            lbl.Foreground = Brushes.White;
            //---- Color del fondo
            brush = (Brush)bc.ConvertFrom("#7F232323");
            lbl.Background = brush;

            lbl.HorizontalAlignment = HorizontalAlignment.Left;
            lbl.Margin = new Thickness(5, 20, 0, 0);
            //lbl.Width = 285.5;
            lbl.Content = str;
            lbl.FontSize = 12;

            return lbl;
        }

        //--- Añade las actividades guardadas al contenedor
        public void displaySavedActivities()
        {
            DateTime temp = DateTime.Now;
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            System.Globalization.Calendar cal = dfi.Calendar;
            grid_Actividades.Children.Clear();

            for (int i = 0; i < listaActividades.Count; i++)
            {
                actividadesControl act = loadListToControl(listaActividades, i);

                if (i > 0)
                    if (act.fechaStart.Date != temp.Date)
                        grid_Actividades.Children.Insert(0, addLabelToContainer(temp.ToLongDateString()));

                //---- Inserta el nuevo control (Actividad)
                act.HorizontalAlignment = HorizontalAlignment.Left;
                act.Margin = new Thickness(5, 10, 0, 8);
                grid_Actividades.Children.Insert(0, act);

                //---- Temporal para poner la etiqueta del día
                temp = act.fechaStart;
            }

            grid_Actividades.Children.Insert(0, addLabelToContainer(temp.ToLongDateString()));
        }

        //---- Devuelve iniciales del nombre que se introduzca
        private string getNameInitials(string name)
        {
            string temp = name.Substring(0, 1);

            for (int i = 0; i < name.Length; i++)
            {
                if (name[i].Equals(' '))
                    temp += name[i + 1];
            }

            return temp.ToUpper();
        }

        //---- Configura los timers
        private void timersSettup()
        {
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            System.Globalization.Calendar cal = dfi.Calendar;

            timer_Notificaciones.Tick += new EventHandler(timer_Notificaciones_tick);
            timer_Notificaciones.Interval = new TimeSpan(notificaciones_modo > 2 ? 4 : notificaciones_modo, 0, 0);

            timer_Recordatorio.Tick += new EventHandler(timer_Recordatorio_tick);
            timer_Recordatorio.Interval = new TimeSpan(0, 20, 0);

            if (notificaciones_modo > 0) timer_Notificaciones.Start();
            else timer_Notificaciones.Stop();
            if (cal.GetDayOfWeek(DateTime.Now).ToString().Equals("Friday") && recordatorio_activo) timer_Recordatorio.Start();
            else timer_Recordatorio.Stop();
        }

        //---- Tarea en segundo plano para generar archivo de excel
        private void generateExcel()
        {
            grid_PantallaDeCarga.Visibility = Visibility.Visible;
            grid_Actividades.Visibility = Visibility.Collapsed;

            Storyboard storyBoard = (Storyboard)this.FindResource("an_Loading");
            storyBoard.Begin();

            //---- Guarda actividades
            saveList();

            BackgroundWorker bw = new BackgroundWorker();

            //---- Proceso que se hará en background
            bw.DoWork += new DoWorkEventHandler(delegate (object o, DoWorkEventArgs args)
            {
                excelSaveProcess();
            });

            //---- Proceso que se hará cuando se termine la tarea
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate (object o, RunWorkerCompletedEventArgs args)
            {
                storyBoard.Stop();
                grid_PantallaDeCarga.Visibility = Visibility.Collapsed;
                grid_Actividades.Visibility = Visibility.Visible;
            });

            bw.RunWorkerAsync();
        }

        //---- Timer para notificaciones
        private void timer_Recordatorio_tick(object sender, EventArgs e)
        {
            if (DateTime.Now.Hour >= 17)
            {
                MessageBox.Show("¡" + nombre_Usuario.Split(' ')[0] + ", recuerda enviar el reporte DAS antes de irte!");
                generateExcel();
            }
        }

        //---- Timer para notificaciones
        private void timer_Notificaciones_tick(object sender, EventArgs e)
        {
            MessageBox.Show("¿Que estas haciendo?");
        }
    }
}