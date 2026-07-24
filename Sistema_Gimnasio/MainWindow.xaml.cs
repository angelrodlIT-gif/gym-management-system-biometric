using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Gym_System.Core;
using Sistema_Gimnasio.Views;


namespace Sistema_Gimnasio
{
  
public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InicioButton.Click += (s, e) => MainContent.Content = new InicioView();
            miembrosButton.Click += (s, e) => MainContent.Content = new MiembrosView();
            membresiasButton.Click += (s, e) => MainContent.Content = new MembresiasView();
            pagosButton.Click += (s, e) => MainContent.Content = new PagosView();
            visitasButton.Click += (s, e) => MainContent.Content = new VisitasView();
            accesoButton.Click += (s, e) =>
            {
                var ventanaAcceso = new SistemaAcceso();
                ventanaAcceso.Show(); 
            };

        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e) 
        {
            if (e.ChangedButton == MouseButton.Left) 
            {
                DragMove();
            }
        }

        private bool isMaximized = false;

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (isMaximized)
                {
                    this.WindowState = WindowState.Normal;
                    this.Width = 1080;
                    this.Height = 720;

                    isMaximized = false;
                }
                else
                {
                    this.WindowState = WindowState.Maximized;

                    isMaximized = true;
                }
            }
        }

        private void miembrosButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void maximizingButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }
        public void ToggleWindowState()
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void closeButton_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void minimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void InicioButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void pagosButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void membresiasButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void visitasButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void acessoButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
