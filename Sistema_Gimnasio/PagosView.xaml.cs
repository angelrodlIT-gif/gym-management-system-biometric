using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Gym_System.Core;

namespace Sistema_Gimnasio
{
    public partial class PagosView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private DateTime _fechaInicio;
        public DateTime FechaInicio
        {
            get => _fechaInicio;
            set
            {
                if (_fechaInicio != value)
                {
                    _fechaInicio = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime _fechaFin;
        public DateTime FechaFin
        {
            get => _fechaFin;
            set
            {
                if (_fechaFin != value)
                {
                    _fechaFin = value;
                    OnPropertyChanged();
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private ObservableCollection<Pagos_agregar> _reporteMembresiasActivas;
        public ObservableCollection<Pagos_agregar> ReporteMembresiasActivas
        {
            get => _reporteMembresiasActivas;
            set
            {
                _reporteMembresiasActivas = value;
                OnPropertyChanged(nameof(ReporteMembresiasActivas));
            }
        }

        private ObservableCollection<Pagos_agregar> _reporteMembresiasFiltradas;
        public ObservableCollection<Pagos_agregar> ReporteMembresiasFiltradas
        {
            get => _reporteMembresiasFiltradas;
            set
            {
                _reporteMembresiasFiltradas = value;
                OnPropertyChanged(nameof(ReporteMembresiasFiltradas));
            }
        }

        public PagosView()
        {
            InitializeComponent();
            DataContext = this;
            CargarDatos(DateTime.Today, DateTime.Today);
        }

        private void CargarDatos(DateTime inicio, DateTime fin)
        {
            Pagos_conexion pagosConexion = new Pagos_conexion();

            // Cargar reporte general (membresías activas)
            ReporteMembresiasActivas = pagosConexion.ObtenerReporteMembresiasActivos();

            // Total general de Membresias
            decimal totalFiltrado = pagosConexion.ObtenerTotalEsperadoPagado();
            txtTotalPagado.Text = totalFiltrado.ToString("C");

            // Cargar filtrado si hay cambio en las fechas
            var datosFiltrados = pagosConexion.ReportePagosConNombreMembresia(inicio, fin);
            ReporteMembresiasFiltradas = datosFiltrados ?? new ObservableCollection<Pagos_agregar>();
            // Total general de visitas
            var (totalVisitas, totalVisitasIds) = pagosConexion.ObtenerTotalesVisitasPorFecha(inicio, fin);
            txtTotalPagadoVisitas.Text = totalVisitas.ToString("C");
            txtTotalVisitas.Text = totalVisitasIds.ToString();

        }

        private void ActualizarEstados_Click(object sender, RoutedEventArgs e)
        {
            FechaInicio = datePickerInicio.SelectedDate ?? DateTime.Today;
            FechaFin = datePickerFin.SelectedDate ?? DateTime.Today;

            CargarDatos(FechaInicio, FechaFin);
        }


        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            FechaInicio = datePickerInicio.SelectedDate ?? DateTime.Today;
            FechaFin = datePickerFin.SelectedDate ?? DateTime.Today;
            CargarDatos(FechaInicio, FechaFin);
        }



        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Primer y último día del mes
            DateTime fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime fechaFin = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));

            datePickerInicio.SelectedDate = fechaInicio;
            datePickerFin.SelectedDate = fechaFin;

            CargarDatos(fechaInicio, fechaFin);
        }

    }
}
