namespace SvSoft.MSBuild.MultiAssemblyPackageCheck;

public static class Consumer2
{
    public static void TriggerAssemblyLoad(Consumer.Foo foo)
    {
    }

    public class Foo
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Bar { get; }
    }
}
