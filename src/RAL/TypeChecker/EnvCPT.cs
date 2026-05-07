namespace RAL.TC;

using System.Diagnostics.Metrics;
using RAL.AST;

//// <summary> categoryId x propertyId -> type. 
/// Typing environment for properties defined within a category.
/// For a given category, a property identifier must always be associated with the same type across all resources in that category.
/// Purpose: Support static type-checking of "where" condition within queries containing a*rc ident </summary>
public class EnvCPT {
    //                       categoryId         { propertyId, TypeT }
    private readonly Dictionary<string, Dictionary<string, TypeT>> CPT = new(); 

    public void Bind(string categoryId, string propertyId, TypeT type) {
        
        //Lazy addition of Category to environment. Only add when a property needs binding
        if(!CPT.ContainsKey(categoryId)) {
            CPT.Add(categoryId, new Dictionary<string, TypeT>());
        } 
        
        //Extract inner map
        Dictionary<string, TypeT> propertyTypeMap = CPT[categoryId];

        //Checked other places, throw exception if reached
        if(propertyTypeMap.ContainsKey(propertyId)) {
            throw new Exception($"Property {propertyId} already exists within Category {categoryId} !");
        }

        propertyTypeMap.Add(propertyId, type);

    }

    //Returns the type 
    public TypeT Lookup(string categoryId, string propertyId) {

        Dictionary<string, TypeT> propertyTypeMap = GetPropertyTypeMap(categoryId);
        
        //Fetch type, throw exception if propertyId is not in the map
        if(!propertyTypeMap.TryGetValue(propertyId, out TypeT type)) {
            throw new Exception("Eror!!");
        }

        return type;
    }

        /// <summary> Returns inner map of categoryId, throws exception if not registered </summary>
    public Dictionary<string, TypeT> GetPropertyTypeMap (string categoryId)
    {
        //Guard invalid categoryId. tryGetValue returns false if dictionary did not contain key 'categoryId'.
        if(CPT.TryGetValue(categoryId, out Dictionary<string, TypeT> propertyTypeMap) == false) {
            throw new Exception($"Invalid categoryId: '{categoryId}' argument!");
        }

        //Key: CategoryID valid, 'propertyTypeMap' contains value: dictionary<propertyId -> type>
        return propertyTypeMap;
    }

    //Boolean function checking wether a property exists within a category
    public bool HasProperty(string categoryId, string propertyId) {
        return HasCategory(categoryId) && GetPropertyTypeMap(categoryId).ContainsKey(propertyId);
    }

    public bool HasCategory(string categoryId)
    {
        return CPT.ContainsKey(categoryId); 
    
    }

}