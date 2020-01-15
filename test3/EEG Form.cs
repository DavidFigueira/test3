using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.IO.Ports;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using System.Linq;


namespace SensorDisplay
{
    public partial class EEGForm : Form
    {
        public EEGForm()
        {
            InitializeComponent();
        }
        EEGControler Controlador = new EEGControler();

        List<medicion> recorded = new List<medicion>(); // lista que guarda las mediciones
        String tag = null; // etiqueta actual a colocar
        private void StartButton_Click(object sender, EventArgs e)
        {

            try
            {
                if (!aSerialPort.IsOpen)
                {
                    LOG.BeginInvoke((MethodInvoker)delegate { LOG.AppendText("Prueba Iniciada \n"); });
                    aSerialPort.Open();
                    aSerialPort.DiscardInBuffer();
                    recorded.Clear();
                    chart1.Series["Series1"].Points.Clear();
                    tag = null;
                }
            }
            catch
            {
                MessageBox.Show("Error, No se encuentra el dispositivo en el puerto " + aSerialPort.PortName);
            }
        }

        private void updatechart(medicion data)
        {
            chart1.Series["Series1"].Points.AddXY(data.etiqueta, data.numvalor);
            recorded.Add(data);
            chart1.ChartAreas[0].AxisX.ScaleView.Position = chart1.Series["Series1"].Points.Count - 500;// ubica siempre la pantalla al final
        }
        // proceso que maneja la llegada de data al puerto serial
        private void serialDataReceivedEventHandler(object sender, SerialDataReceivedEventArgs e)
        {
            try // en caso de que ya este proceso haya iniciado y se precione el boton de cerrar el puerto, produce un error
            {
                SerialPort sData = sender as SerialPort;
                string recvData = sData.ReadLine();

                //LOG.BeginInvoke((MethodInvoker)delegate { LOG.AppendText("Received: " + recvData); });

                // initialization of chart update
                double data;
                bool result = Double.TryParse(recvData, out data);
                if (result)
                {
                    medicion m = new medicion(recvData, tag, data);
                    if (tag == null)
                    {
                        m = new medicion(recvData, " ", data);
                    }
                    else
                    {
                        m = new medicion(recvData, tag, data);
                        tag = null;
                    }
                    //datarecorded.Add(recvData);
                    this.Invoke((MethodInvoker)delegate { updatechart(m); });

                }
            }
            catch
            {
                //MessageBox.Show("failed");//do nothing ... mensaje de prueba
            }
        }
        public class medicion // clase medicion, tiene String valor, y etiqueta para guardar en txt, y double para la grafica
        {
            public medicion(string valor, string etiqueta, double numvalor)
            {
                this.valor = valor;
                this.etiqueta = etiqueta;
                this.numvalor = numvalor;
            }

            public String valor { get; set; }
            public String etiqueta { get; set; }
            public Double numvalor { get; set; }
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            chart1.ChartAreas[0].AxisX.Minimum = 0;
            chart1.ChartAreas[0].AxisY.Maximum = 1100;
            chart1.ChartAreas[0].AxisY.Minimum = 0;

            // se inicializa el puerto con las opciones basicas
            aSerialPort.PortName = "COM3";
            aSerialPort.BaudRate = 115200;
            aSerialPort.Parity = Parity.None;
            aSerialPort.StopBits = StopBits.One;
            aSerialPort.DataBits = 8;
            aSerialPort.DataReceived += new SerialDataReceivedEventHandler(serialDataReceivedEventHandler);

            // se inicializa la Chart, eliminando los bordes
            chart1.ChartAreas[0].AxisX.LineWidth = 0;
            chart1.ChartAreas[0].AxisY.LineWidth = 0;
            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MinorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisX.MajorTickMark.Enabled = false;
            chart1.ChartAreas[0].AxisY.MajorTickMark.Enabled = false;
            chart1.ChartAreas[0].AxisX.MinorTickMark.Enabled = false;
            chart1.ChartAreas[0].AxisY.MinorTickMark.Enabled = false;
            chart1.ChartAreas[0].AxisY.LabelStyle.Enabled = false;

            chart1.ChartAreas[0].AxisX.Interval = 1;
            chart1.ChartAreas[0].AxisY.Interval = 1;

            chart1.ChartAreas[0].AxisX.LineColor = Color.Transparent;
            chart1.ChartAreas[0].AxisY.LineColor = Color.Transparent;

            chart1.ChartAreas[0].AxisY.ScrollBar.Size = 10;
            chart1.ChartAreas[0].AxisY.ScrollBar.ButtonStyle = ScrollBarButtonStyles.SmallScroll;
            chart1.ChartAreas[0].AxisY.ScrollBar.IsPositionedInside = true;
            chart1.ChartAreas[0].AxisY.ScrollBar.Enabled = true;

            chart1.ChartAreas[0].AxisX.ScrollBar.IsPositionedInside = true;
            chart1.ChartAreas[0].AxisX.ScrollBar.Enabled = true;
            chart1.ChartAreas[0].AxisX.ScaleView.Size = 500; //la porcion o cantidad de muestras que se ven en pantalla

        }
        // 
        // Click en boton de start, abre el puerto, permitiendo la llegada de datos lo que dispara el update de la Chart y la data
        private void StopButton_Click(object sender, EventArgs e)
        {
            if (aSerialPort.IsOpen)
            {
                Thread CloseSerial = new Thread(new ThreadStart(CloseSerialm)); //close port in new thread to avoid hang
                CloseSerial.Start(); //close port in new thread to avoid hang
                LOG.BeginInvoke((MethodInvoker)delegate { LOG.AppendText("Prueba Detenida \n"); });
                // pruebaencurso = false;
            }
        }

        // click en boton de shock, envia un mensaje al arduino.
        private void ShockButton_Click(object sender, EventArgs e)
        {
            if (aSerialPort.IsOpen)
            {
                aSerialPort.WriteLine("a");
                tag = "shock";
            }
            else
            {
                MessageBox.Show("Debe haber una prueba corriendo para poder activar un impulso");
            }
        }

        /// <summary>
        /// A continuacion, procesos necesarios para cerrar el puerto serial
        /// por la forma en que funciona el puerto serial, al intentar cerrarlo en el mismo thread del programa, produce un 
        /// deadlock, por lo que los siguientes procesos, crean Threads paralelos que evitan que esto suceda
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)

        {
            if (aSerialPort.IsOpen)
            {
                e.Cancel = true; //detener el cerrado de la Form
                Thread CloseDown = new Thread(new ThreadStart(CloseSerialOnExit)); //cerrar el puerto en otro thread para evitar deadlock
                CloseDown.Start(); //inicia el thread anteriormente definido ^
            }
        }

        private void CloseSerialm() //metodo para cerrar el puerto serial (por el boton)
        {
            try
            {
                aSerialPort.Close(); //cerrar el puerto
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); //catch cualquier error
            }
        }
        private void CloseSerialOnExit() // metodo para cerrar el serial on exit (al final cierra la Form)

        {
            try
            {
                aSerialPort.Close(); //cierra el puerto
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); //catch cualquier error
            }
            this.Invoke(new EventHandler(NowClose)); //now close back in the main thread
        }

        private void NowClose(object sender, EventArgs e)
        {
            this.Close(); //cierra la Form
        }
        // fin del cierre del puerto

        // el cambio de texto en la combobox cambia el puerto de la comunicacion serial
        private void ComPortComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            aSerialPort.PortName = ComPortComboBox.Text;
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            recorded.Clear();
            chart1.Series["Series1"].Points.Clear();
            LOG.BeginInvoke((MethodInvoker)delegate { LOG.AppendText("Datos despejados \n"); });
        }

        private void TagButton_Click(object sender, EventArgs e)
        {
            tag = TagTextBox.Text;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {

            Controlador.EEG_Save(this.saveFileDialog1, this.LOG);

            //if (aSerialPort.IsOpen)
            //{
            //    LOG.BeginInvoke((MethodInvoker)delegate { LOG.AppendText("Error, detenga la prueba antes de guardar \n"); });
            //}
            //else
            //{
            //    try
            //    {
            //        if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            //        {
            //            if (File.Exists(saveFileDialog1.FileName))
            //            {
            //                File.Delete(saveFileDialog1.FileName);
            //            }
            //        }
            //        using (StreamWriter sw = File.CreateText(saveFileDialog1.FileName))
            //        {
            //            foreach (medicion m in recorded)
            //            {
            //                sw.WriteLine(m.valor.TrimEnd() + "," + m.etiqueta);
            //            }
            //        }
            //    }
            //    catch //(Exception Ex)
            //    {
            //        //MessageBox.Show(Ex.ToString());
            //        MessageBox.Show("Error Inesperado Escribiendo Archivo"); ;
            //    }

            //}


        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            Controlador.EEG_Load(this.openFileDialog1, this.LOG, this.chart1);

            //if (!aSerialPort.IsOpen)
            //{
            //    try
            //    {
            //        if (openFileDialog1.ShowDialog() == DialogResult.OK)// si el archivo se abrio bien
            //        {
            //            //codigo para leer el archivo
            //            if (openFileDialog1.FileNames.Length > 1) //para evitar que se abran multiples archivos, en caso de que el usuario encuentre una forma
            //            {
            //                MessageBox.Show("Error, solo se puede cargar un archivo a la vez");
            //            }
            //            else
            //            {
            //                recorded.Clear();
            //                chart1.Series["Series1"].Points.Clear();
            //                //==recorded.Clear(); // borra lo que haya
            //                using (StreamReader sr = File.OpenText(openFileDialog1.FileName))
            //                {
            //                    string reading = "";
            //                    while ((reading = sr.ReadLine()) != null)
            //                    {
            //                        string[] datos = reading.Split(',');
            //                        double data;
            //                        Double.TryParse(datos[0], out data);
            //                        medicion m = new medicion(datos[0], datos[1], data);
            //                        recorded.Add(m);
            //                        updatechart(m);
            //                    }
            //                }
            //                //codigo para leer
            //            }
            //        }
            //    }
            //    catch
            //    {
            //        MessageBox.Show("Error en lectura archivo");
            //        recorded.Clear();
            //    }
            }

        private void LOG_TextChanged(object sender, EventArgs e) // para que cada vez que se agregue algo al log, se vea en pantalla
        {
            // set the current caret position to the end
            LOG.SelectionStart = LOG.Text.Length;
            // scroll it automatically
            LOG.ScrollToCaret();
        }

        
    }
}
