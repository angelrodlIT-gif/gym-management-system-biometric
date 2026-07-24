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
using Gym_System.Core;
using LiveCharts;
using LiveCharts.Wpf;

namespace Sistema_Gimnasio
{
    /// <summary>
    /// Interaction logic for InicioView.xaml
    /// </summary>
    public partial class InicioView : UserControl
    {
        private Pagos_conexion pagosConexion = new Pagos_conexion();
        public InicioView()
        {
            InitializeComponent();
            CargarGraficoSemanaActual();
            CargarDatos();
            CargarGraficoMensual();
            DataContext = this;
            CargarPagosRecientes();



        }

        /// Método para cargar los datos en el gráfico
        public SeriesCollection SeriesCollection { get; set; }
        public SeriesCollection SeriesCollection1 { get; set; }
        public SeriesCollection SeriesCollection2 { get; set; }

        public string[] LabelsSemanaActual { get; set; }
        public string[] LabelsSemanasMes { get; set; }
        public string[] LabelsMeses { get; set; }

        public void CargarDatos()
        {
            var datos = pagosConexion.ObtenerPagosPorSemanaDelMesActual();

            LabelsSemanaActual = datos.Select(d => d.Semana).ToArray();
            var valores = datos.Select(d => (double)d.Total).ToArray();

            //Añadir datos a los textos
            decimal totalFiltrado = pagosConexion.ObtenerTotal();
            txtTotalPagado.Text = totalFiltrado.ToString("C");
            decimal totalFiltradoDias = pagosConexion.ObtenerTotalDias();
            txtTotalPagadoDias.Text = totalFiltradoDias.ToString("C");
            decimal totalFiltradoSemana = pagosConexion.ObtenerTotalSemana();
            txtTotalPagadoSemana.Text = totalFiltradoSemana.ToString("C");
            decimal totalFiltradoMeses = pagosConexion.ObtenerTotalMes();
            txtTotalPagadoMes.Text = totalFiltradoMeses.ToString("C");

                


            SeriesCollection = new SeriesCollection
        {
                new LineSeries 
                {
                    Values = new ChartValues<double>(valores),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 5
                }
        };
                }

        //Grafico Semanal por día
        public void CargarGraficoSemanaActual()
        {
            var datos = pagosConexion.ObtenerPagosSemanaActual();

            var diasOrdenados = new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };

            var diccionario = datos.ToDictionary(d => d.Dia, d => d.Total);

            LabelsSemanasMes = diasOrdenados; //Bindeado a SeriesCollection1
            var valores = diasOrdenados.Select(d => diccionario.ContainsKey(d) ? (double)diccionario[d] : 0).ToArray();

            SeriesCollection1 = new SeriesCollection
    {
        new LineSeries
        {
            Values = new ChartValues<double>(valores),
            PointGeometry = DefaultGeometries.Circle, 
            PointGeometrySize = 5                    
        }
    };
        }

        //Gráfico Mensual por año
        public void CargarGraficoMensual()
        {
            var datos = pagosConexion.ObtenerPagosPorMesDeCadaAnio();

            var meses = new[]
            {
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    };

            LabelsMeses = meses;

            var datosPorAnio = datos.GroupBy(d => d.Anio)
                .ToDictionary(g => g.Key, g => g.ToDictionary(m => m.Mes, m => m.Total));

            SeriesCollection2 = new SeriesCollection();

            foreach (var anio in datosPorAnio.Keys.OrderBy(x => x))
            {
                var valores = new ChartValues<double>();

                for (int mes = 1; mes <= 12; mes++)
                {
                    valores.Add(datosPorAnio[anio].ContainsKey(mes) ? (double)datosPorAnio[anio][mes] : 0);
                }

                SeriesCollection2.Add(new LineSeries
                {
                    Title = anio.ToString(),
                    Values = valores,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 5
                });
            }
        }

        private void CargarPagosRecientes()
        {
            var conexion = new Pagos_conexion();
            icPagosRecientes.ItemsSource = conexion.ObtenerPagosRecientes();
        }

    }
}
