using System;
using System.Collections.Generic;

namespace WinCtl {

class PriorityQueue<T> {

  // Internal vars
  ///////////////////////

  public List<(T, float)> entries = new List<(T, float)>();
  public float offset;

  // Public methods
  ///////////////////////

  public void Push(T data, float value) {
    value += offset;
    var insertIdx = 0;
    var boundIdx = entries.Count;
    while (insertIdx < boundIdx) {
      var middleIdx = (int)((insertIdx + boundIdx) / 2);
      var (_, middleValue) = entries[middleIdx];
      if (value < middleValue - offset) {
        boundIdx = middleIdx;
      } else {
        insertIdx = middleIdx + 1;
      }
    }
    entries.Insert(insertIdx, (data, value));
  }

  public (T, float) Peek() {
    var (data, value) = entries[0];
    return (data, value - offset);
  }

  public (T, float) Pop() {
    var entry = Peek();
    entries.RemoveAt(0);
    return entry;
  }

  public void Adjust(float delta) {
    offset += delta;
  }

  public void Clear() {
    entries.Clear();
    offset = 0;
  }

  public bool IsEmpty() {
    return entries.Count == 0;
  }

}

}
