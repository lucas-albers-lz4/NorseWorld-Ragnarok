import nwr.game.NWGameSpace;
import jzrlib.core.FileVersion;

/**
 * Headless dump of Java save-format constants for C# differential testing (#18).
 * Compile/run via: ./dev_info/ab-java-constants.sh
 */
public class DumpJavaConstants {
    public static void main(String[] args) {
        FileVersion v = NWGameSpace.RGF_Version;
        System.out.println("Release=" + v.Release);
        System.out.println("Revision=" + v.Revision);
        System.out.println("RGP=" + new String(NWGameSpace.RGP_Sign));
        System.out.println("RGT=" + new String(NWGameSpace.RGT_Sign));
        System.out.println("SAVEFILE_PLAYER=" + NWGameSpace.SAVEFILE_PLAYER);
        System.out.println("SAVEFILE_TERRAINS=" + NWGameSpace.SAVEFILE_TERRAINS);
        System.out.println("SAVEFILE_JOURNAL=" + NWGameSpace.SAVEFILE_JOURNAL);
    }
}
