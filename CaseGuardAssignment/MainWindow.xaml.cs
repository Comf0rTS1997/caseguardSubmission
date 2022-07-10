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

namespace CaseGuardAssignment
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int toolSelec; // 0 = Pen, 1 = Eraser, 3 = Modify
        private Uri? imagePath;
        // color
        private byte r_value;
        private byte g_value;
        private byte b_value;

        // current selected rectangle
        private Rectangle? currentSelect;

        public MainWindow()
        {
            toolSelec = 0;
            // color
            r_value = 0;
            g_value = 0;
            b_value = 0;
            InitializeComponent();
            colorMonitor.Fill = new SolidColorBrush(Color.FromRgb(r_value, g_value, b_value));
        }

        private void Open_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".jpg";
            bool? success = dialog.ShowDialog();
            if (success == true)
            {
                imagePath = new Uri(dialog.FileName);
                BitmapImage imgbm = new BitmapImage(imagePath);
                ImageBrush backgroundBrush = new ImageBrush(imgbm);
                backgroundBrush.Stretch = Stretch.Uniform;
                imgCanvas.Background = backgroundBrush;
                saveMenu.IsEnabled = true;
            }
        }
        private void Save_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelect != null)
            {
                currentSelect.StrokeThickness = 0;
                currentSelect.Stroke = null;
                currentSelect = null;
            }
            RenderTargetBitmap bmp = new RenderTargetBitmap((int)imgCanvas.ActualWidth, (int)imgCanvas.ActualHeight, 100.0, 100.0, PixelFormats.Default);
            bmp.Render(imgCanvas);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = "png";
            dialog.Filter = "Image (*.png)|*.png";
            dialog.AddExtension = true;
            bool? success = dialog.ShowDialog();
            if(success == true)
            {
                System.IO.FileStream stream = System.IO.File.Create(dialog.FileName);
                encoder.Save(stream);
                stream.Close();
            }
        }

        private void Pen_Click(object sender, RoutedEventArgs e)
        {
            toolSelec = 0;
            updateControl();
        }

        private void Eraser_Click(object sender, RoutedEventArgs e)
        {
            toolSelec = 1;
            updateControl();
        }

        private void Modify_Click(object sender, RoutedEventArgs e)
        {
            toolSelec = 2;
            updateControl();
        }

        private void redSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            r_value = (byte) redSlider.Value;
            updateColorMonitor();
            updateSelectRecColor();
        }

        private void greenSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            g_value = (byte)greenSlider.Value;
            updateColorMonitor();
            updateSelectRecColor();
        }

        private void blueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            b_value = (byte)blueSlider.Value;
            updateColorMonitor();
            updateSelectRecColor();
        }

        private void imgCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (toolSelec){
                case 0: // pen mode
                    Rectangle r = new Rectangle
                    {
                        Height = 100,
                        Width = 100,
                        Fill = new SolidColorBrush(Color.FromRgb(r_value, g_value, b_value)),
                    };
                    r.MouseMove += new MouseEventHandler(Rectangle_MouseMove);
                    r.MouseWheel += new MouseWheelEventHandler(Rectangle_MouseWheel);
                    Canvas.SetTop(r, Mouse.GetPosition(imgCanvas).Y);
                    Canvas.SetLeft(r, Mouse.GetPosition(imgCanvas).X);
                    imgCanvas.Children.Add(r);
                    break;
                case 1: // eraser mode
                    if (e.Source is Rectangle)
                    {
                        imgCanvas.Children.Remove((Rectangle)e.Source);
                    }
                    break;
                case 2: // modify mode
                    if (e.Source is Rectangle)
                    {
                        cancelSelection();
                        currentSelect = (Rectangle)e.Source;
                        currentSelect.StrokeThickness = 10;
                        currentSelect.Stroke = Brushes.Red;
                        SolidColorBrush sc = (SolidColorBrush)currentSelect.Fill;
                        r_value = sc.Color.R;
                        g_value = sc.Color.G;
                        b_value = sc.Color.B;
                        updateColorMonitor();
                    }
                    else
                    {
                        cancelSelection();
                    }
                    break;
            }
           
        }
        // handle drag'n'drop
        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if(toolSelec == 2)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    // drag and drop whole rectangle
                    DragDrop.DoDragDrop(currentSelect, e.Source, DragDropEffects.Move);
                }
            }
            
        }
        private void imgCanvas_DragOver(object sender, DragEventArgs e)
        {
            Point loc = e.GetPosition(imgCanvas);
            Canvas.SetLeft(currentSelect, loc.X);
            Canvas.SetTop(currentSelect, loc.Y);
        }

        // handle size change
        private void Rectangle_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((toolSelec == 2) && (e.Source is Rectangle) &&(e.Source == currentSelect) 
                                   && (currentSelect != null))
            {
                int mode = 2;
                if(e.MiddleButton == MouseButtonState.Pressed)
                {
                    mode = 0;
                }else if(e.RightButton == MouseButtonState.Pressed)
                {
                    mode = 1;
                }
                switch (mode)
                {
                    case 0: // change height
                        if(currentSelect.Height + (e.Delta * 0.1) > 0)
                        {
                            currentSelect.Height += e.Delta * 0.1;
                        }
                        break;
                    case 1: // change width
                        if (currentSelect.Width + (e.Delta * 0.1) > 0)
                        {
                            currentSelect.Width += e.Delta * 0.1;
                        }
                        break;
                    case 2:// change both height and width
                        if ((currentSelect.Width + (e.Delta * 0.1) > 0) 
                            && (currentSelect.Height + (e.Delta * 0.1) > 0))
                        {
                            currentSelect.Width += e.Delta * 0.1;
                            currentSelect.Height += e.Delta * 0.1;
                        }
                        break;
                }
            }
        }

        // update bottom control highlight
        private void updateControl()
        {
            switch (toolSelec) { 
                case 0:
                    PenButton.Opacity = 0.5;
                    EraserButton.Opacity = 1.0;
                    ModifyButton.Opacity = 1.0;
                    cancelSelection();
                    break;
                case 1:
                    PenButton.Opacity = 1.0;
                    EraserButton.Opacity = 0.5;
                    ModifyButton.Opacity = 1.0;
                    cancelSelection();
                    break;
                case 2:
                    PenButton.Opacity = 1.0;
                    EraserButton.Opacity = 1.0;
                    ModifyButton.Opacity = 0.5;
                    saveMenu.IsEnabled = false;
                    break;
            }
        }

        private void updateSelectRecColor()
        {
            if (currentSelect != null)
            {
                currentSelect.Fill = new SolidColorBrush(Color.FromRgb(r_value, g_value, b_value));
            }
        }

        // update bottom color monitor and global color picker
        private void updateColorMonitor()
        {
            colorMonitor.Fill = new SolidColorBrush(Color.FromRgb(r_value, g_value, b_value));
            redSlider.Value = (int)r_value;
            greenSlider.Value = (int)g_value;
            blueSlider.Value = (int)b_value;
        }

        private void cancelSelection()
        {
            if(currentSelect != null)
            {
                currentSelect.StrokeThickness = 0;
                currentSelect.Stroke = null;
                currentSelect = null;
                saveMenu.IsEnabled = true;
            }
        }
    }
}
