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
    internal class EEGControler
    {
        SerialPort aSerialPort = new SerialPort();
        List<medicion> recorded = new List<medicion>(); // lista que guarda las mediciones
        String tag = null; // etiqueta actual a colocar
        public EEGControler()
        {
            aSerialPort.PortName = "COM3";
            aSerialPort.BaudRate = 9600;
            aSerialPort.ReadBufferSize = 4095;
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

        public void EEG_Save(SaveFileDialog saveFileDialog1, RichTextBox LOG)
        {

            if (aSerialPort.IsOpen)
            {
                LOG.BeginInvoke((MethodInvoker)delegate { LOG.AppendText("Error, detenga la prueba antes de guardar \n"); });
            }
            else
            {
                try
                {
                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        if (File.Exists(saveFileDialog1.FileName))
                        {
                            File.Delete(saveFileDialog1.FileName);
                        }
                    }
                    using (StreamWriter sw = File.CreateText(saveFileDialog1.FileName))
                    {
                        foreach (medicion m in recorded)
                        {
                            sw.WriteLine(m.valor.TrimEnd() + "," + m.etiqueta);
                        }
                    }
                }
                catch //(Exception Ex)
                {
                    //MessageBox.Show(Ex.ToString());
                    MessageBox.Show("Error Inesperado Escribiendo Archivo"); ;
                }

            }


        }

        public void EEG_Load(OpenFileDialog openFileDialog1, RichTextBox LOG, Chart chart1)
        {
            if (!aSerialPort.IsOpen)
            {
                try
                {
                    if (openFileDialog1.ShowDialog() == DialogResult.OK)// si el archivo se abrio bien
                    {
                        //codigo para leer el archivo
                        if (openFileDialog1.FileNames.Length > 1) //para evitar que se abran multiples archivos, en caso de que el usuario encuentre una forma
                        {
                            MessageBox.Show("Error, solo se puede cargar un archivo a la vez");
                        }
                        else
                        {
                            recorded.Clear();
                            chart1.Series["Series1"].Points.Clear();
                            //==recorded.Clear(); // borra lo que haya
                            using (StreamReader sr = File.OpenText(openFileDialog1.FileName))
                            {
                                string reading = "";
                                while ((reading = sr.ReadLine()) != null)
                                {
                                    string[] datos = reading.Split(',');
                                    double data;
                                    Double.TryParse(datos[0], out data);
                                    medicion m = new medicion(datos[0], datos[1], data);
                                    recorded.Add(m);
                                    updatechart(m);
                                }
                            }
                            //codigo para leer
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("Error en lectura archivo");
                    recorded.Clear();
                }
            }
    }
}