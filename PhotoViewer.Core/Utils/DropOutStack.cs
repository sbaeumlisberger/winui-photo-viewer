using System.Diagnostics.CodeAnalysis;

namespace PhotoViewer.Core.Utils;

class DropOutStack<T> where T : notnull
{
    private T[] array;
    private int top = -1;
    private int count = 0;

    public DropOutStack(int capacity)
    {
        array = new T[capacity];
    }

    public void Push(T item)
    {
        top = (top + 1) % array.Length;
        array[top] = item;       
        count++;
    }

    public bool TryPop([NotNullWhen(true)] out T? element)
    {
        if (count <= 0) 
        {
            element = default;
            return false;
        }
        element = array[top];
        top = (array.Length + top - 1) % array.Length;  
        count--;
        return true;
    }
}
