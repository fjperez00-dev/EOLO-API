using System.IO;

namespace TaskApiClient_WPF.Models
{
    public class TaskExecutionSend
    {
        public string UnitCode { get; set; }
        public string TaskCode { get; set; }
        public string FunCode { get; set; }
        public DateTime TaskDate { get; set; }
        public string TaskExecutionStatusCode { get; set; }
        public byte[] Imagen { get; set; }

        // Propiedad auxiliar para mostrar la imagen redimensionada
        public System.Windows.Media.Imaging.BitmapImage ImagenBitmap
        {
            get
            {
                if (Imagen == null || Imagen.Length == 0)
                    return null;

                using (var ms = new MemoryStream(Imagen))
                {
                    var image = new System.Windows.Media.Imaging.BitmapImage();
                    image.BeginInit();
                    image.DecodePixelWidth = 128;
                    image.DecodePixelHeight = 128;
                    image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
        }
    }
}
