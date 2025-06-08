using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemonMaxwellGame
{
    public class TouchableGraphicsView : GraphicsView
    {
        public event EventHandler<TouchPointEventArgs> TouchStart;
        public event EventHandler<TouchPointEventArgs> TouchMove;
        public event EventHandler<TouchPointEventArgs> TouchEnd;

        public class TouchPointEventArgs : EventArgs
        {
            public Vect2 Pos { get; set; }
        }

#if ANDROID
        // Register the Android handler for this control
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (Handler?.PlatformView is Android.Views.View nativeView)
            {
                nativeView.Touch += OnTouchEvent;
            }
        }

        private void OnTouchEvent(object sender, Android.Views.View.TouchEventArgs e)
        {
            var motionEvent = e.Event;

            if (motionEvent.Action == Android.Views.MotionEventActions.Down)
            {
                TouchStart?.Invoke(this, new TouchPointEventArgs { Pos = new Vect2(motionEvent.GetX(), motionEvent.GetY()) });
            }
            else if (motionEvent.Action == Android.Views.MotionEventActions.Move)
            {
                TouchMove?.Invoke(this, new TouchPointEventArgs { Pos = new Vect2(motionEvent.GetX(), motionEvent.GetY()) });
            }
            else if (motionEvent.Action == Android.Views.MotionEventActions.Up)
            {
                TouchEnd?.Invoke(this, new TouchPointEventArgs { Pos = new Vect2(motionEvent.GetX(), motionEvent.GetY()) });
            }
            // Pass the event to the base implementation
            e.Handled = false;
        }
#endif
    }
}