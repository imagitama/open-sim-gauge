using System.Text;

namespace OpenGaugeServer
{
    public class TeeTextWriter : TextWriter
    {
        private readonly TextWriter _original;
        private readonly TextWriter _log;

        public TeeTextWriter(TextWriter original, TextWriter log)
        {
            _original = original;
            _log = log;
        }

        public override Encoding Encoding => _original.Encoding;

        public override void WriteLine(string? value)
        {
            _original.WriteLine(value);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            _log.WriteLine($"[{timestamp}] {value}");
        }

        public override void Write(char value)
        {
            _original.Write(value);
            _log.Write(value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _log.Dispose();
                _original.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}