namespace Properties;

[System.AttributeUsage(System.AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
sealed class SentryDSN : System.Attribute
{
  public string ConfigurationLocation { get; }

  public SentryDSN(string sentryDsn)
  {
    this.ConfigurationLocation = sentryDsn;
  }
}