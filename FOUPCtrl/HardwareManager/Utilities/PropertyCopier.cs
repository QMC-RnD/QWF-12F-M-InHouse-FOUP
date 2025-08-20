using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.HardwareManager.Utilities
{
    public static class PropertiesCopier
    {
        /// <summary>
        /// Extension for 'Object' that copies the properties with public get and set accessors to a destination object.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        public static void Copy(this object source, object destination, Type propertyAttribute = null)
        {
            // If any this null throw an exception
            if (source == null || destination == null)
            {
                throw new Exception("Source or/and Destination Objects are null");
            }

            // Getting the Types of the objects
            Type typeDest = destination.GetType();
            Type typeSrc = source.GetType();

            // Iterate the Properties of the source instance and  
            // populate them from their desination counterparts  
            PropertyInfo[] sourceProperties = typeSrc.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo sourceProperty in sourceProperties)
            {
                if (!sourceProperty.CanRead)
                {
                    continue;
                }

                PropertyInfo targetProperty = typeDest.GetProperty(sourceProperty.Name, BindingFlags.Public | BindingFlags.Instance);
                if (targetProperty == null)
                {
                    throw new ArgumentException($"Property <{sourceProperty.Name}> is not present and accessible in {source.GetType().FullName}");
                }

                if (propertyAttribute != null)
                {
                    var attribute = targetProperty.GetCustomAttribute(propertyAttribute, true);
                    if (attribute == null)
                    {
                        continue;
                    }
                }

                if (!targetProperty.CanWrite)
                {
#if LOG
                    Debug.WriteLine("Property <{0}> is not writable in <{1}>", sourceProperty.Name, source.GetType().FullName);
#endif
                    continue;
                }

                if (targetProperty.GetSetMethod(true)?.IsPublic != true)
                {
#if LOG
                    Debug.WriteLine("Property <{0}> setter is not public in <{1}>", sourceProperty.Name, source.GetType().FullName);
#endif
                    continue;
                }

                if ((targetProperty.GetSetMethod(true).Attributes & MethodAttributes.Static) != 0)
                {
                    throw new ArgumentException($"Property <{sourceProperty.Name}> is static in {source.GetType().FullName}");
                }

                if (!targetProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                {
                    throw new ArgumentException($"Property <{sourceProperty.Name}> has an incompatible type in {source.GetType().FullName}");
                }

                // Passed all tests, lets set the value

                // Handle List properties for deep cloning
                if (typeof(System.Collections.IList).IsAssignableFrom(sourceProperty.PropertyType))
                {
                    var sourceList = (System.Collections.IList)sourceProperty.GetValue(source, null);
                    if (sourceList != null)
                    {
                        var targetList = (System.Collections.IList)Activator.CreateInstance(targetProperty.PropertyType);
                        foreach (var item in sourceList)
                        {
                            // Deep clone each item

                            var clonedItem = Activator.CreateInstance(item.GetType());
                            item.Copy(clonedItem);
                            //if (item != null && !item.GetType().IsPrimitive && !(item is string))
                            //{
                            //    var cloneMethod = item.GetType().GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
                            //    clonedItem = cloneMethod.Invoke(item, null);
                            //}
                            targetList.Add(clonedItem);
                        }
                        targetProperty.SetValue(destination, targetList, null);
                    }
                }
                else if (sourceProperty.PropertyType.IsClass && sourceProperty.PropertyType != typeof(string))
                {
                    // Handle deep cloning of reference types (non-primitive, non-string)
                    var sourceValue = sourceProperty.GetValue(source, null);
                    if (sourceValue != null)
                    {
                        var targetValue = Activator.CreateInstance(sourceProperty.PropertyType);
                        sourceValue.Copy(targetValue); // Recursively deep copy
                        targetProperty.SetValue(destination, targetValue, null);
                    }
                }
                else
                    targetProperty.SetValue(destination, sourceProperty.GetValue(source, null), null);
            }
        }
    }
}
