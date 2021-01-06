using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno {
    internal class CircularQueue<T> : Queue<double> {
        private int MaxSize;

        public CircularQueue(int maxSize) {
            MaxSize = maxSize;
        }

        public void Enqueue(double sample) {
            while (base.Count >= MaxSize)
                base.Dequeue();
            base.Enqueue(sample);
        }
        public void SetSize(int maxSize) => MaxSize = MaxSize;

    }
}
