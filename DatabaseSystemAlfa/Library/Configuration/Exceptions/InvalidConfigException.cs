namespace DatabaseSystemAlfa.Library.Configuration.Exceptions;

public class InvalidConfigException(string message, string sectionName) : Exception(string.Format(message, sectionName));