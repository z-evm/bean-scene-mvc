using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeanScene.Models;

public class Person
{
    public Person(string name)
    {
        Name = name;
    }
    public int Id { get; set; }
    public string Name { get; set; }
}
