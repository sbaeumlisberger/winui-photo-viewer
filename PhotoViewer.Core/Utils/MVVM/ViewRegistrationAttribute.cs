namespace PhotoViewer.App.Utils;

public class ViewRegistrationAttribute : Attribute
{

    public Type ViewModelType { get; }

    public ViewRegistrationAttribute(Type viewModelType)
    {
        ViewModelType = viewModelType;
    }

}

