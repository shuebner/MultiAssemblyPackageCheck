namespace SvSoft.MSBuild.MultiAssemblyPackageCheck;

public static class Consumer
{
    public static void TriggerAssemblyLoad(Foo foo)
    {
    }

    public class Foo
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Bar { get; }
    }
}
