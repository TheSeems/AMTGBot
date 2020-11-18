using System.IO;

namespace AMTGBot
{
    public interface ILatexRenderer
    {
        Stream Render(string latex);
    }
}
