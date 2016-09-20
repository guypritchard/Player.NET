namespace Player.Db
{
    using System;
    using System.Windows;
    using System.Windows.Controls;

    public class ElasticWrapPanel : Panel
    {
        /// <summary>
        /// Identifies the <see cref="DesiredColumnWidth"/> dependency property. 
        /// </summary>
        internal static readonly DependencyProperty DesiredColumnWidthProperty = DependencyProperty.Register("DesiredColumnWidth", typeof(double), typeof(ElasticWrapPanel), new PropertyMetadata(100d, new PropertyChangedCallback(OnDesiredColumnWidthChanged)));

        private int _columns;

        protected override Size MeasureOverride(Size availableSize)
        {
            if (this.Visibility == Visibility.Visible)
            {
                _columns = (int) (availableSize.Width/DesiredColumnWidth);

                foreach (UIElement item in this.InternalChildren)
                {
                    item.Measure(availableSize);
                }
            }

            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_columns != 0)
            {
                double columnWidth = Math.Floor(finalSize.Width / _columns);

                double top = 0;
                double rowHeight = 0;
                int column = 0;
                foreach (UIElement item in this.Children)
                {
                    item.Arrange(new Rect(columnWidth * column, top, columnWidth, item.DesiredSize.Height));
                    column++;
                    rowHeight = Math.Max(rowHeight, item.DesiredSize.Height);

                    if (column == _columns)
                    {
                        column = 0;
                        top += rowHeight;
                        rowHeight = 0;
                    }
                }
            }

            return base.ArrangeOverride(finalSize);
        }

        private static void OnDesiredColumnWidthChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var panel = (ElasticWrapPanel)obj;
            panel.InvalidateMeasure();
            panel.InvalidateArrange();
        }

        public double DesiredColumnWidth
        {
            get
            {
                return (double)GetValue(DesiredColumnWidthProperty);
            }

            set
            {
                SetValue(DesiredColumnWidthProperty, value);
            }
        }
    }

}
