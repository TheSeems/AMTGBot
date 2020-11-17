using System.IO;

namespace AMTGBot
{
    interface ILatexRenderer
    {
        Stream Render(string latex);
    }
}
