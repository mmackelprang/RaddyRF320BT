package com.myhomesmartlife.bluetooth.CleanedUp;

/**
 * Rate Type Information
 * 
 * Represents a rate/mode type configuration for the radio.
 * Contains hex value, display name, and bit representation.
 */
public class RateTypeInfo {
    
    /** Hexadecimal type identifier */
    private String hexType;
    
    /** Human-readable name/description */
    private String name;
    
    /** Bit pattern representation */
    private String bitType;
    
    /**
     * Default constructor
     */
    public RateTypeInfo() {
    }
    
    /**
     * Full constructor
     * 
     * @param hexType Hexadecimal type identifier
     * @param name Display name
     * @param bitType Bit pattern
     */
    public RateTypeInfo(String hexType, String name, String bitType) {
        this.hexType = hexType;
        this.name = name;
        this.bitType = bitType;
    }
    
    // Getters
    
    public String getHexType() {
        return hexType;
    }
    
    public String getName() {
        return name;
    }
    
    public String getBitType() {
        return bitType;
    }
    
    // Setters
    
    public void setHexType(String hexType) {
        this.hexType = hexType;
    }
    
    public void setName(String name) {
        this.name = name;
    }
    
    public void setBitType(String bitType) {
        this.bitType = bitType;
    }
    
    @Override
    public String toString() {
        return "RateTypeInfo{" +
                "hexType='" + hexType + '\'' +
                ", name='" + name + '\'' +
                ", bitType='" + bitType + '\'' +
                '}';
    }
}
