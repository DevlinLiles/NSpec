﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NSpec.Domain.Extensions;

namespace NSpec.Domain
{
    public class Context
    {
        public Type Type { get; set; }

        public void AddExample(Example example)
        {
            example.Context = this;
            Examples.Add(example);
        }

        public void Afters()
        {
            if (After != null)
                After();
        }

        public void Befores()
        {
            if (Parent != null )
                Parent.Befores();

            if (Before != null)
                Before();
        }

        public void Acts()
        {
            if (Parent != null)
                Parent.Acts();

            if (Act != null)
                Act();
        }

        public IEnumerable<Example> AllExamples()
        {
            return Contexts.Examples().Union(Examples);
        }

        public Context(string name="") : this(name,0) { }

        public Context(string name, int level)
        {
            Name = name.Replace("_", " ");
            Level = level;
            Examples = new List<Example>();
            Contexts = new ContextCollection();
        }

        protected MethodInfo Method { get; set; }

        public string Name { get; set; }
        public int Level { get; set; }
        public List<Example> Examples { get; set; }
        public ContextCollection Contexts { get; set; }
        public Action Before { get; set; }
        public Action Act { get; set; }
        public Action After { get; set; }
        public Context Parent { get; set; }

        public IEnumerable<Example> Failures()
        {
            return AllExamples().Where(e => e.Exception != null);
        }

        public void AddContext(Context child)
        {
            child.Parent = this;
            Contexts.Add(child);
        }

        public Action<nspec> BeforeInstance { get; set; }

        public Action<nspec> ActInstance { get; set; }

        public void SetInstanceContext(nspec instance)
        {
            if (BeforeInstance != null) Before = () => BeforeInstance(instance);

            if (ActInstance != null) Act = () => ActInstance(instance);

            if(Parent!=null) Parent.SetInstanceContext(instance);
        }

        public IEnumerable<Context> SelfAndDescendants()
        {
            return new[] { this }.Concat(Contexts.SelectMany(c => c.SelfAndDescendants()));
        }

        public void Run()
        {
            Contexts.Do(c => c.Run() );

            if (Method != null)
            {
                var instance = GetSpecType().Instance<nspec>();

                SetInstanceContext(instance);

                instance.Context = this;

                try
                {
                    Method.Invoke(instance, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception executing context: {0}".With(FullContext()));

                    throw e;
                }
            }
        }

        private Type GetSpecType()
        {
            if (Type != null) return Type;

            return Parent.GetSpecType();
        }

        public string FullContext()
        {
            if (Parent != null)
                return Parent.FullContext() + ". " + Name;

            return Name;
        }
    }
}