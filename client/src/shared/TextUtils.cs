namespace OpenGaugeClient
{
    public static class TextUtils
    {
        public static double GetHorizontalOffset(double parentWidth, double textWidth, TextHorizontalAlignment align)
        {
            return align switch
            {
                TextHorizontalAlignment.Left => 0,
                TextHorizontalAlignment.Center => (parentWidth / 2) - (textWidth / 2),
                TextHorizontalAlignment.Right => parentWidth - textWidth,
                _ => (parentWidth / 2) - (textWidth / 2),
            };
        }

        public static double GetVerticalOffset(double parentHeight, double textHeight, TextVerticalAlignment align)
        {
            return align switch
            {
                TextVerticalAlignment.Top => 0,
                TextVerticalAlignment.Center => (parentHeight / 2) - (textHeight / 2),
                TextVerticalAlignment.Bottom => parentHeight - textHeight,
                _ => (parentHeight / 2) - (textHeight / 2),
            };
        }
    }
}