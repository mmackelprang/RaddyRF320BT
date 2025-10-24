package com.myhomesmartlife.bluetooth.CleanedUp;

/**
 * Remark/Memo Information
 * 
 * Represents a saved memory/preset entry for the radio.
 * Contains frequency, band, demodulation rate, and other channel parameters.
 */
public class RemarkInfo {
    
    /** Band identifier (e.g., "VHF", "UHF") */
    private String band;
    
    /** Demodulation rate/type */
    private String decRate;
    
    /** Data byte 4 */
    private String byte4;
    
    /** Data byte 5 */
    private String byte5;
    
    /** Data byte 6 */
    private String byte6;
    
    /** Data byte 7 */
    private String byte7;
    
    /** User-assigned name/label for this preset */
    private String name;
    
    /**
     * Default constructor
     */
    public RemarkInfo() {
    }
    
    /**
     * Full constructor
     * 
     * @param band Band identifier
     * @param decRate Demodulation rate
     * @param byte4 Data byte 4
     * @param byte5 Data byte 5
     * @param byte6 Data byte 6
     * @param byte7 Data byte 7
     * @param name User-assigned name
     */
    public RemarkInfo(String band, String decRate, String byte4, String byte5, 
                      String byte6, String byte7, String name) {
        this.band = band;
        this.decRate = decRate;
        this.byte4 = byte4;
        this.byte5 = byte5;
        this.byte6 = byte6;
        this.byte7 = byte7;
        this.name = name;
    }
    
    // Getters
    
    public String getBand() {
        return band;
    }
    
    public String getDecRate() {
        return decRate;
    }
    
    public String getByte4() {
        return byte4;
    }
    
    public String getByte5() {
        return byte5;
    }
    
    public String getByte6() {
        return byte6;
    }
    
    public String getByte7() {
        return byte7;
    }
    
    public String getName() {
        return name;
    }
    
    // Setters
    
    public void setBand(String band) {
        this.band = band;
    }
    
    public void setDecRate(String decRate) {
        this.decRate = decRate;
    }
    
    public void setByte4(String byte4) {
        this.byte4 = byte4;
    }
    
    public void setByte5(String byte5) {
        this.byte5 = byte5;
    }
    
    public void setByte6(String byte6) {
        this.byte6 = byte6;
    }
    
    public void setByte7(String byte7) {
        this.byte7 = byte7;
    }
    
    public void setName(String name) {
        this.name = name;
    }
    
    @Override
    public String toString() {
        return "RemarkInfo{" +
                "band='" + band + '\'' +
                ", decRate='" + decRate + '\'' +
                ", byte4='" + byte4 + '\'' +
                ", byte5='" + byte5 + '\'' +
                ", byte6='" + byte6 + '\'' +
                ", byte7='" + byte7 + '\'' +
                ", name='" + name + '\'' +
                '}';
    }
}
