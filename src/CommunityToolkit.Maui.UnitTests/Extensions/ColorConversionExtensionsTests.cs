using CommunityToolkit.Maui.Core.Extensions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class ColorConversionExtensionsTests : BaseTest
{
	const double tolerance = 0.000001;

	public static IReadOnlyList<object[]> ColorTestData { get; } =
	[
		[new ColorTestDefinition((float)0.5019608, 0, 0, 0, 128, 0, 0, 0, (float)0.498039186000824, 0, 1, 1, (float)0.498039215686, 1, 1, (float)0.167320261, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.54509807, 0, 0, 0, 139, 0, 0, 0, (float)0.45490193367004395, 0, 1, 1, (float)0.454901961, 1, 1, (float)0.181699346, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.64705884, (float)0.16470589, (float)0.16470589, 0, 165, 42, 42, 0, (float)0.3529411554336548, 0, (float)0.7454545689023223, (float)0.7454545689023223, (float)0.352941176, (float)0.835294118, (float)0.835294118, (float)0.325490196, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.69803923, (float)0.13333334, (float)0.13333334, 0, 178, 34, 34, 0, (float)0.3019607663154602, 0, (float)0.80898878035514, (float)0.80898878035514, (float)0.301960784, (float)0.866666667, (float)0.866666667, (float)0.321568627, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.8627451, (float)0.078431375, (float)0.23529412, 0, 220, 20, 60, 0, (float)0.13725489377975464, 0, (float)0.9090909153715637, (float)0.7272727461146913, (float)0.137254902, (float)0.921568627, (float)0.764705882, (float)0.392156863, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(1, 0, 0, 0, 255, 0, 0, 0, 0, 0, 1, 1, 0, 1, 1, (float)0.333333333, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(1, (float)0.3882353, (float)0.2784314, 0, 255, 99, 71, 0, 0, 0, (float)0.611764669418335, (float)0.7215685844421387, 0, (float)0.611764706, (float)0.721568627, (float)0.555555556, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.49803922, (float)0.3137255, 0, 255, 127, 80, 0, 0, 0, (float)0.5019607543945312, (float)0.686274528503418, 0, (float)0.501960784, (float)0.68627451, (float)0.603921569, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.8039216, (float)0.36078432, (float)0.36078432, 0, 205, 92, 92, 0, (float)0.19607841968536377, 0, (float)0.5512195454687673, (float)0.5512195454687673, (float)0.196078431, (float)0.639215686, (float)0.639215686, (float)0.508496732, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.9411765, (float)0.5019608, (float)0.5019608, 0, 240, 128, 128, 0, (float)0.05882352590560913, 0, (float)0.46666663711269707, (float)0.46666663711269707, (float)0.058823529, (float)0.498039216, (float)0.498039216, (float)0.648366013, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.9137255, (float)0.5882353, (float)0.47843137, 0, 233, 150, 122, 0, (float)0.08627450466156006, 0, (float)0.35622315272831884, (float)0.47639488394150725, (float)0.08627451, (float)0.411764706, (float)0.521568627, (float)0.660130719, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.98039216, (float)0.5019608, (float)0.44705883, 0, 250, 128, 114, 0, (float)0.019607841968536377, 0, (float)0.4879999703311921, (float)0.5440000277233124, (float)0.019607843, (float)0.498039216, (float)0.552941176, (float)0.643137255, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.627451, (float)0.47843137, 0, 255, 160, 122, 0, 0, 0, (float)0.37254899740219116, (float)0.5215686559677124, 0, (float)0.37254902, (float)0.521568627, (float)0.701960784, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.27058825, 0, 0, 255, 69, 0, 0, 0, 0, (float)0.7294117212295532, 1, 0, (float)0.729411765, 1, (float)0.423529412, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(1, (float)0.54901963, 0, 0, 255, 140, 0, 0, 0, 0, (float)0.45098036527633667, 1, 0, (float)0.450980392, 1, (float)0.516339869, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.64705884, 0, 0, 255, 165, 0, 0, 0, 0, (float)0.3529411554336548, 1, 0, (float)0.352941176, 1, (float)0.549019608, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.84313726, 0, 0, 255, 215, 0, 0, 0, 0, (float)0.15686273574829102, 1, 0, (float)0.156862745, 1, (float)0.614379085, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.72156864, (float)0.5254902, (float)0.043137256, 0, 184, 134, 11, 0, (float)0.27843135595321655, 0, (float)0.2717391079879725, (float)0.940217396242646, (float)0.278431373, (float)0.474509804, (float)0.956862745, (float)0.430065359, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.85490197, (float)0.64705884, (float)0.1254902, 0, 218, 165, 32, 0, (float)0.1450980305671692, 0, (float)0.24311924910452054, (float)0.8532110194085913, (float)0.145098039, (float)0.352941176, (float)0.874509804, (float)0.54248366, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.93333334, (float)0.9098039, (float)0.6666667, 0, 238, 232, 170, 0, (float)0.06666666269302368, 0, (float)0.025210082423644056, (float)0.28571426746796597, (float)0.066666667, (float)0.090196078, (float)0.333333333, (float)0.836601307, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.7411765, (float)0.7176471, (float)0.41960785, 0, 189, 183, 107, 0, (float)0.2588235139846802, 0, (float)0.03174602919304878, (float)0.4338623989716666, (float)0.258823529, (float)0.282352941, (float)0.580392157, (float)0.626143791, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.9411765, (float)0.9019608, (float)0.54901963, 0, 240, 230, 140, 0, (float)0.05882352590560913, 0, (float)0.04166666402791938, (float)0.4166666402791938, (float)0.058823529, (float)0.098039216, (float)0.450980392, (float)0.797385621, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.5019608, (float)0.5019608, 0, 0, 128, 128, 0, 0, (float)0.498039186000824, 0, 0, 1, (float)0.498039216, (float)0.498039216, 1, (float)0.334640523, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(1, 1, 0, 0, 255, 255, 0, 0, 0, 0, 0, 1, 0, 0, 1, (float)0.666666667, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.6039216, (float)0.8039216, (float)0.19607843, 0, 154, 205, 50, 0, (float)0.19607841968536377, (float)0.24878046935970508, 0, (float)0.7560975790591127, (float)0.396078431, (float)0.196078431, (float)0.803921569, (float)0.534640523, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.33333334, (float)0.41960785, (float)0.18431373, 0, 85, 107, 47, 0, (float)0.5803921222686768, (float)0.20560744742929254, 0, (float)0.5607477259465113, (float)0.666666667, (float)0.580392157, (float)0.815686275, (float)0.3124183, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.41960785, (float)0.5568628, (float)0.13725491, 0, 107, 142, 35, 0, (float)0.4431372284889221, (float)0.2464788468571995, 0, (float)0.7535211531428005, (float)0.580392157, (float)0.443137255, (float)0.862745098, (float)0.37124183, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.4862745, (float)0.9882353, 0, 0, 124, 252, 0, 0, (float)0.011764705181121826, (float)0.5079365376149355, 0, 1, (float)0.51372549, (float)0.011764706, 1, (float)0.491503268, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.49803922, 1, 0, 0, 127, 255, 0, 0, 0, (float)0.5019607543945312, 0, 1, (float)0.501960784, 0, 1, (float)0.499346405, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.6784314, 1, (float)0.18431373, 0, 173, 255, 47, 0, 0, (float)0.3215686082839966, 0, (float)0.8156862854957581, (float)0.321568627, 0, (float)0.815686275, (float)0.620915033, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(0, (float)0.39215687, 0, 0, 0, 100, 0, 0, (float)0.6078431606292725, 1, 0, 1, 1, (float)0.607843137, 1, (float)0.130718954, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, (float)0.5019608, 0, 0, 0, 128, 0, 0, (float)0.498039186000824, 1, 0, 1, 1, (float)0.498039216, 1, (float)0.167320261, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.13333334, (float)0.54509807, (float)0.13333334, 0, 34, 139, 34, 0, (float)0.45490193367004395, (float)0.7553957101998988, 0, (float)0.7553957101998988, (float)0.866666667, (float)0.454901961, (float)0.866666667, (float)0.270588235, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, 1, 0, 0, 0, 255, 0, 0, 0, 1, 0, 1, 1, 0, 1, (float)0.333333333, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.19607843, (float)0.8039216, (float)0.19607843, 0, 50, 205, 50, 0, (float)0.19607841968536377, (float)0.7560975790591127, 0, (float)0.7560975790591127, (float)0.803921569, (float)0.196078431, (float)0.803921569, (float)0.39869281, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.5647059, (float)0.93333334, (float)0.5647059, 0, 144, 238, 144, 0, (float)0.06666666269302368, (float)0.39495795797042355, 0, (float)0.39495795797042355, (float)0.435294118, (float)0.066666667, (float)0.435294118, (float)0.687581699, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.59607846, (float)0.9843137, (float)0.59607846, 0, 152, 251, 152, 0, (float)0.0156862735748291, (float)0.3944222868729186, 0, (float)0.3944222868729186, (float)0.403921569, (float)0.015686275, (float)0.403921569, (float)0.725490196, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.56078434, (float)0.7372549, (float)0.56078434, 0, 143, 188, 143, 0, (float)0.26274508237838745, (float)0.23936168277605013, 0, (float)0.23936168277605013, (float)0.439215686, (float)0.262745098, (float)0.439215686, (float)0.619607843, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(0, (float)0.98039216, (float)0.6039216, 0, 0, 250, 154, 0, (float)0.019607841968536377, 1, 0, (float)0.3839999766540528, 1, (float)0.019607843, (float)0.396078431, (float)0.528104575, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(0, 1, (float)0.49803922, 0, 0, 255, 127, 0, 0, 1, 0, (float)0.5019607543945312, 1, 0, (float)0.501960784, (float)0.499346405, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.18039216, (float)0.54509807, (float)0.34117648, 0, 46, 139, 87, 0, (float)0.45490193367004395, (float)0.6690647843880984, 0, (float)0.3741006785178019, (float)0.819607843, (float)0.454901961, (float)0.658823529, (float)0.355555556, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.4, (float)0.8039216, (float)0.6666667, 0, 102, 205, 170, 0, (float)0.19607841968536377, (float)0.5024390612805898, 0, (float)0.17073169465862112, (float)0.6, (float)0.196078431, (float)0.333333333, (float)0.623529412, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.23529412, (float)0.7019608, (float)0.44313726, 0, 60, 179, 113, 0, (float)0.29803919792175293, (float)0.6648044977357461, 0, (float)0.36871505249067926, (float)0.764705882, (float)0.298039216, (float)0.556862745, (float)0.460130719, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.1254902, (float)0.69803923, (float)0.6666667, 0, 32, 178, 170, 0, (float)0.3019607663154602, (float)0.8202247344518965, 0, (float)0.044943816387025874, (float)0.874509804, (float)0.301960784, (float)0.333333333, (float)0.496732026, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.18431373, (float)0.30980393, (float)0.30980393, 0, 47, 79, 79, 0, (float)0.6901960372924805, (float)0.4050634056019184, 0, 0, (float)0.815686275, (float)0.690196078, (float)0.690196078, (float)0.267973856, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, (float)0.5019608, (float)0.5019608, 0, 0, 128, 128, 0, (float)0.498039186000824, 1, 0, 0, 1, (float)0.498039216, (float)0.498039216, (float)0.334640523, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, (float)0.54509807, (float)0.54509807, 0, 0, 139, 139, 0, (float)0.45490193367004395, 1, 0, 0, 1, (float)0.454901961, (float)0.454901961, (float)0.363398693, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, 1, 1, 0, 0, 255, 255, 0, 0, 1, 0, 0, 1, 0, 0, (float)0.666666667, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(0, 1, 1, 0, 0, 255, 255, 0, 0, 1, 0, 0, 1, 0, 0, (float)0.666666667, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.8784314, 1, 1, 0, 224, 255, 255, 0, 0, (float)0.12156862020492554, 0, 0, (float)0.121568627, 0, 0, (float)0.959477124, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(0, (float)0.80784315, (float)0.81960785, 0, 0, 206, 209, 0, (float)0.18039214611053467, 1, (float)0.014354065941769816, 0, 1, (float)0.192156863, (float)0.180392157, (float)0.54248366, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.2509804, (float)0.8784314, (float)0.8156863, 0, 64, 224, 208, 0, (float)0.12156862020492554, (float)0.714285733672429, 0, (float)0.07142856658189277, (float)0.749019608, (float)0.121568627, (float)0.184313725, (float)0.648366013, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.28235295, (float)0.81960785, (float)0.8, 0, 72, 209, 204, 0, (float)0.18039214611053467, (float)0.6555024173975245, 0, (float)0.023923443236283027, (float)0.717647059, (float)0.180392157, (float)0.2, (float)0.633986928, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.6862745, (float)0.93333334, (float)0.93333334, 0, 175, 238, 238, 0, (float)0.06666666269302368, (float)0.2647058654482626, 0, 0, (float)0.31372549, (float)0.066666667, (float)0.066666667, (float)0.850980392, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.49803922, 1, (float)0.83137256, 0, 127, 255, 212, 0, 0, (float)0.5019607543945312, 0, (float)0.16862744092941284, (float)0.501960784, 0, (float)0.168627451, (float)0.776470588, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.6901961, (float)0.8784314, (float)0.9019608, 0, 176, 224, 230, 0, (float)0.09803920984268188, (float)0.23478259318041678, (float)0.026086954797824084, 0, (float)0.309803922, (float)0.121568627, (float)0.098039216, (float)0.823529412, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.37254903, (float)0.61960787, (float)0.627451, 0, 95, 158, 160, 0, (float)0.37254899740219116, (float)0.4062499614083222, (float)0.01249999881256376, 0, (float)0.62745098, (float)0.380392157, (float)0.37254902, (float)0.539869281, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.27450982, (float)0.50980395, (float)0.7058824, 0, 70, 130, 180, 0, (float)0.29411762952804565, (float)0.6111111439488545, (float)0.27777775432224683, 0, (float)0.725490196, (float)0.490196078, (float)0.294117647, (float)0.496732026, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.39215687, (float)0.58431375, (float)0.92941177, 0, 100, 149, 237, 0, (float)0.07058823108673096, (float)0.578059098789696, (float)0.3713079930650675, 0, (float)0.607843137, (float)0.415686275, (float)0.070588235, (float)0.635294118, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(0, (float)0.7490196, 1, 0, 0, 191, 255, 0, 0, 1, (float)0.2509803771972656, 0, 1, (float)0.250980392, 0, (float)0.583006536, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.11764706, (float)0.5647059, 1, 0, 30, 144, 255, 0, 0, (float)0.8823529481887817, (float)0.43529409170150757, 0, (float)0.882352941, (float)0.435294118, 0, (float)0.560784314, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.6784314, (float)0.84705883, (float)0.9019608, 0, 173, 216, 230, 0, (float)0.09803920984268188, (float)0.24782607057932882, (float)0.060869561194922865, 0, (float)0.321568627, (float)0.152941176, (float)0.098039216, (float)0.809150327, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.5294118, (float)0.80784315, (float)0.92156863, 0, 135, 206, 235, 0, (float)0.07843136787414551, (float)0.4255318873713276, (float)0.123404247337685, 0, (float)0.470588235, (float)0.192156863, (float)0.078431373, (float)0.752941176, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.5294118, (float)0.80784315, (float)0.98039216, 0, 135, 206, 250, 0, (float)0.019607841968536377, (float)0.4599999720335007, (float)0.17599998929977417, 0, (float)0.470588235, (float)0.192156863, (float)0.019607843, (float)0.77254902, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.09803922, (float)0.09803922, (float)0.4392157, 0, 25, 25, 112, 0, (float)0.5607843399047852, (float)0.7767857142857143, (float)0.7767857142857143, 0, (float)0.901960784, (float)0.901960784, (float)0.560784314, (float)0.211764706, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, 0, (float)0.5019608, 0, 0, 0, 128, 0, (float)0.498039186000824, 1, 1, 0, 1, 1, (float)0.498039216, (float)0.167320261, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, 0, (float)0.54509807, 0, 0, 0, 139, 0, (float)0.45490193367004395, 1, 1, 0, 1, 1, (float)0.454901961, (float)0.181699346, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, 0, (float)0.8039216, 0, 0, 0, 205, 0, (float)0.19607841968536377, 1, 1, 0, 1, 1, (float)0.196078431, (float)0.267973856, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, 0, 1, 0, 0, 0, 255, 0, 0, 1, 1, 0, 1, 1, 0, (float)0.333333333, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.25490198, (float)0.4117647, (float)0.88235295, 0, 65, 105, 225, 0, (float)0.11764705181121826, (float)0.7111110630741829, (float)0.5333332973056373, 0, (float)0.745098039, (float)0.588235294, (float)0.117647059, (float)0.516339869, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.5411765, (float)0.16862746, (float)0.8862745, 0, 138, 43, 226, 0, (float)0.11372548341751099, (float)0.3893805047864316, (float)0.8097345260702664, 0, (float)0.458823529, (float)0.831372549, (float)0.11372549, (float)0.532026144, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.29411766, 0, (float)0.50980395, 0, 75, 0, 130, 0, (float)0.4901960492134094, (float)0.42307687361212454, 1, 0, (float)0.705882353, 1, (float)0.490196078, (float)0.267973856, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.28235295, (float)0.23921569, (float)0.54509807, 0, 72, 61, 139, 0, (float)0.45490193367004395, (float)0.48201444512919744, (float)0.5611511271233478, 0, (float)0.717647059, (float)0.760784314, (float)0.454901961, (float)0.355555555, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.41568628, (float)0.3529412, (float)0.8039216, 0, 106, 90, 205, 0, (float)0.19607841968536377, (float)0.48292686760531883, (float)0.5609756423064028, 0, (float)0.584313725, (float)0.647058824, (float)0.196078431, (float)0.524183007, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.48235294, (float)0.40784314, (float)0.93333334, 0, 123, 104, 238, 0, (float)0.06666666269302368, (float)0.48319324645317774, (float)0.5630252379901697, 0, (float)0.517647059, (float)0.592156863, (float)0.066666667, (float)0.607843137, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.5764706, (float)0.4392157, (float)0.85882354, 0, 147, 112, 219, 0, (float)0.14117646217346191, (float)0.32876710047035573, (float)0.4885845103794466, 0, (float)0.423529412, (float)0.560784314, (float)0.141176471, (float)0.624836601, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.54509807, 0, (float)0.54509807, 0, 139, 0, 139, 0, (float)0.45490193367004395, 0, 1, 0, (float)0.454901961, 1, (float)0.454901961, (float)0.363398693, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.5803922, 0, (float)0.827451, 0, 148, 0, 211, 0, (float)0.17254900932312012, (float)0.29857817754433624, 1, 0, (float)0.419607843, 1, (float)0.17254902, (float)0.469281046, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.6, (float)0.19607843, (float)0.8, 0, 153, 50, 204, 0, (float)0.19999998807907104, (float)0.24999998137354879, (float)0.7549019790455405, 0, (float)0.4, (float)0.803921569, (float)0.2, (float)0.532026144, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.7294118, (float)0.33333334, (float)0.827451, 0, 186, 85, 211, 0, (float)0.17254900932312012, (float)0.11848340378743502, (float)0.5971563550886725, 0, (float)0.270588235, (float)0.666666667, (float)0.17254902, (float)0.630065359, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.5019608, 0, (float)0.5019608, 0, 128, 0, 128, 0, (float)0.498039186000824, 0, 1, 0, (float)0.498039216, 1, (float)0.498039216, (float)0.334640523, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.84705883, (float)0.7490196, (float)0.84705883, 0, 216, 191, 216, 0, (float)0.15294116735458374, 0, (float)0.11574073259645905, 0, (float)0.152941176, (float)0.250980392, (float)0.152941176, (float)0.814379085, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.8666667, (float)0.627451, (float)0.8666667, 0, 221, 160, 221, 0, (float)0.13333332538604736, 0, (float)0.2760180805644798, 0, (float)0.133333333, (float)0.37254902, (float)0.133333333, (float)0.786928105, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.93333334, (float)0.50980395, (float)0.93333334, 0, 238, 130, 238, 0, (float)0.06666666269302368, 0, (float)0.453781483625593, 0, (float)0.066666667, (float)0.490196078, (float)0.066666667, (float)0.792156863, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, 0, 1, 0, 255, 0, 255, 0, 0, 0, 1, 0, 0, 1, 0, (float)0.666666667, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.85490197, (float)0.4392157, (float)0.8392157, 0, 218, 112, 214, 0, (float)0.1450980305671692, 0, (float)0.4862385679300698, (float)0.01834862257392608, (float)0.145098039, (float)0.560784314, (float)0.160784314, (float)0.711111111, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.78039217, (float)0.08235294, (float)0.52156866, 0, 199, 21, 133, 0, (float)0.21960783004760742, 0, (float)0.8944723698690151, (float)0.3316582661259525, (float)0.219607843, (float)0.917647059, (float)0.478431373, (float)0.461437908, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.85882354, (float)0.4392157, (float)0.5764706, 0, 219, 112, 147, 0, (float)0.14117646217346191, 0, (float)0.4885845103794466, (float)0.32876710047035573, (float)0.141176471, (float)0.560784314, (float)0.423529412, (float)0.624836601, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.078431375, (float)0.5764706, 0, 255, 20, 147, 0, 0, 0, (float)0.9215686321258545, (float)0.42352938652038574, 0, (float)0.921568627, (float)0.423529412, (float)0.551633987, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.4117647, (float)0.7058824, 0, 255, 105, 180, 0, 0, 0, (float)0.5882352590560913, (float)0.29411762952804565, 0, (float)0.588235294, (float)0.294117647, (float)0.705882353, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.7137255, (float)0.75686276, 0, 255, 182, 193, 0, 0, 0, (float)0.2862744927406311, (float)0.24313724040985107, 0, (float)0.28627451, (float)0.243137255, (float)0.823529412, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.7529412, (float)0.79607844, 0, 255, 192, 203, 0, 0, 0, (float)0.24705880880355835, (float)0.20392155647277832, 0, (float)0.247058824, (float)0.203921569, (float)0.849673202, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.98039216, (float)0.92156863, (float)0.84313726, 0, 250, 235, 215, 0, (float)0.019607841968536377, 0, (float)0.059999996352195745, (float)0.13999999148845674, (float)0.019607843, (float)0.078431373, (float)0.156862745, (float)0.91503268, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9607843, (float)0.9607843, (float)0.8627451, 0, 245, 245, 220, 0, (float)0.039215683937072754, 0, 0, (float)0.10204080999617476, (float)0.039215686, (float)0.039215686, (float)0.137254902, (float)0.928104575, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.89411765, (float)0.76862746, 0, 255, 228, 196, 0, 0, 0, (float)0.10588234663009644, (float)0.23137253522872925, 0, (float)0.105882353, (float)0.231372549, (float)0.887581699, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.92156863, (float)0.8039216, 0, 255, 235, 205, 0, 0, 0, (float)0.07843136787414551, (float)0.19607841968536377, 0, (float)0.078431373, (float)0.196078431, (float)0.908496732, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9607843, (float)0.87058824, (float)0.7019608, 0, 245, 222, 179, 0, (float)0.039215683937072754, 0, (float)0.09387754519648078, (float)0.2693877383899014, (float)0.039215686, (float)0.129411765, (float)0.298039216, (float)0.844444444, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.972549, (float)0.8627451, 0, 255, 248, 220, 0, 0, 0, (float)0.027450978755950928, (float)0.13725489377975464, 0, (float)0.02745098, (float)0.137254902, (float)0.945098039, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.98039216, (float)0.8039216, 0, 255, 250, 205, 0, 0, 0, (float)0.019607841968536377, (float)0.19607841968536377, 0, (float)0.019607843, (float)0.196078431, (float)0.928104575, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.98039216, (float)0.98039216, (float)0.8235294, 0, 250, 250, 210, 0, (float)0.019607841968536377, 0, 0, (float)0.15999999027252199, (float)0.019607843, (float)0.019607843, (float)0.176470588, (float)0.928104575, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, 1, (float)0.8784314, 0, 255, 255, 224, 0, 0, 0, 0, (float)0.12156862020492554, 0, 0, (float)0.121568627, (float)0.959477124, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.54509807, (float)0.27058825, (float)0.07450981, 0, 139, 69, 19, 0, (float)0.45490193367004395, 0, (float)0.5035970672355025, (float)0.8633093674646494, (float)0.454901961, (float)0.729411765, (float)0.925490196, (float)0.296732026, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.627451, (float)0.32156864, (float)0.1764706, 0, 160, 82, 45, 0, (float)0.37254899740219116, 0, (float)0.4875000486848859, (float)0.7187500267173154, (float)0.37254902, (float)0.678431373, (float)0.823529412, (float)0.375163398, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.8235294, (float)0.4117647, (float)0.11764706, 0, 210, 105, 30, 0, (float)0.1764705777168274, 0, (float)0.4999999638114661, (float)0.8571428674824383, (float)0.176470588, (float)0.588235294, (float)0.882352941, (float)0.450980392, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.8039216, (float)0.52156866, (float)0.24705882, 0, 205, 133, 63, 0, (float)0.19607841968536377, 0, (float)0.35121948615487775, (float)0.692682949614482, (float)0.196078431, (float)0.478431373, (float)0.752941176, (float)0.524183007, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.95686275, (float)0.6431373, (float)0.3764706, 0, 244, 164, 96, 0, (float)0.04313725233078003, 0, (float)0.32786883203549533, (float)0.6065574015574056, (float)0.043137255, (float)0.356862745, (float)0.623529412, (float)0.658823529, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.87058824, (float)0.72156864, (float)0.5294118, 0, 222, 184, 135, 0, (float)0.1294117569923401, 0, (float)0.1711711594519722, (float)0.3918918650610942, (float)0.129411765, (float)0.278431373, (float)0.470588235, (float)0.707189542, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.8235294, (float)0.7058824, (float)0.54901963, 0, 210, 180, 140, 0, (float)0.1764705777168274, 0, (float)0.14285713251756174, (float)0.3333333092076441, (float)0.176470588, (float)0.294117647, (float)0.450980392, (float)0.692810458, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.7372549, (float)0.56078434, (float)0.56078434, 0, 188, 143, 143, 0, (float)0.26274508237838745, 0, (float)0.23936168277605013, (float)0.23936168277605013, (float)0.262745098, (float)0.439215686, (float)0.439215686, (float)0.619607843, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.89411765, (float)0.70980394, 0, 255, 228, 181, 0, 0, 0, (float)0.10588234663009644, (float)0.2901960611343384, 0, (float)0.105882353, (float)0.290196078, (float)0.867973856, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.87058824, (float)0.6784314, 0, 255, 222, 173, 0, 0, 0, (float)0.1294117569923401, (float)0.3215686082839966, 0, (float)0.129411765, (float)0.321568627, (float)0.849673203, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.85490197, (float)0.7254902, 0, 255, 218, 185, 0, 0, 0, (float)0.1450980305671692, (float)0.2745097875595093, 0, (float)0.145098039, (float)0.274509804, (float)0.860130719, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.89411765, (float)0.88235295, 0, 255, 228, 225, 0, 0, 0, (float)0.10588234663009644, (float)0.11764705181121826, 0, (float)0.105882353, (float)0.117647059, (float)0.925490196, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.9411765, (float)0.9607843, 0, 255, 240, 245, 0, 0, 0, (float)0.05882352590560913, (float)0.039215683937072754, 0, (float)0.058823529, (float)0.039215686, (float)0.967320262, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.98039216, (float)0.9411765, (float)0.9019608, 0, 250, 240, 230, 0, (float)0.019607841968536377, 0, (float)0.039999997568130496, (float)0.07999999513626099, (float)0.019607843, (float)0.058823529, (float)0.098039216, (float)0.941176471, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.99215686, (float)0.9607843, (float)0.9019608, 0, 253, 245, 230, 0, (float)0.00784313678741455, 0, (float)0.03162055146005288, (float)0.09090908544765203, (float)0.007843137, (float)0.039215686, (float)0.098039216, (float)0.951633987, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.9372549, (float)0.8352941, 0, 255, 239, 213, 0, 0, 0, (float)0.0627450942993164, (float)0.16470587253570557, 0, (float)0.062745098, (float)0.164705882, (float)0.924183007, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.9607843, (float)0.93333334, 0, 255, 245, 238, 0, 0, 0, (float)0.039215683937072754, (float)0.06666666269302368, 0, (float)0.039215686, (float)0.066666667, (float)0.964705882, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9607843, 1, (float)0.98039216, 0, 245, 255, 250, 0, 0, (float)0.039215683937072754, 0, (float)0.019607841968536377, (float)0.039215686, 0, (float)0.019607843, (float)0.980392157, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.4392157, (float)0.5019608, (float)0.5647059, 0, 112, 128, 144, 0, (float)0.43529409170150757, (float)0.22222230431657874, (float)0.1111110993833459, 0, (float)0.560784314, (float)0.498039216, (float)0.435294118, (float)0.501960784, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.46666667, (float)0.53333336, (float)0.6, 0, 119, 136, 153, 0, (float)0.3999999761581421, (float)0.22222220014642874, (float)0.11111110007321437, 0, (float)0.533333333, (float)0.466666667, (float)0.4, (float)0.533333333, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.6901961, (float)0.76862746, (float)0.87058824, 0, 176, 196, 222, 0, (float)0.1294117569923401, (float)0.20720719302080845, (float)0.11711710909871781, 0, (float)0.309803922, (float)0.231372549, (float)0.129411765, (float)0.776470588, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9019608, (float)0.9019608, (float)0.98039216, 0, 230, 230, 250, 0, (float)0.019607841968536377, (float)0.07999999513626099, (float)0.07999999513626099, 0, (float)0.098039216, (float)0.098039216, (float)0.019607843, (float)0.928104575, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.98039216, (float)0.9411765, 0, 255, 250, 240, 0, 0, 0, (float)0.019607841968536377, (float)0.05882352590560913, 0, (float)0.019607843, (float)0.058823529, (float)0.973856209, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9411765, (float)0.972549, 1, 0, 240, 248, 255, 0, 0, (float)0.05882352590560913, (float)0.027450978755950928, 0, (float)0.058823529, (float)0.02745098, 0, (float)0.97124183, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.972549, (float)0.972549, 1, 0, 248, 248, 255, 0, 0, (float)0.027450978755950928, (float)0.027450978755950928, 0, (float)0.02745098, (float)0.02745098, 0, (float)0.981699347, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9411765, 1, (float)0.9411765, 0, 240, 255, 240, 0, 0, (float)0.05882352590560913, 0, (float)0.05882352590560913, (float)0.058823529, 0, (float)0.058823529, (float)0.960784314, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, 1, (float)0.9411765, 0, 255, 255, 240, 0, 0, 0, 0, (float)0.05882352590560913, 0, 0, (float)0.058823529, (float)0.980392157, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9411765, 1, 1, 0, 240, 255, 255, 0, 0, (float)0.05882352590560913, 0, 0, (float)0.058823529, 0, 0, (float)0.980392157, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.98039216, (float)0.98039216, 0, 255, 250, 250, 0, 0, 0, (float)0.019607841968536377, (float)0.019607841968536377, 0, (float)0.019607843, (float)0.019607843, (float)0.986928105, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1, 1, 0, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.4117647, (float)0.4117647, (float)0.4117647, 0, 105, 105, 105, 0, (float)0.5882352590560913, 0, 0, 0, (float)0.588235294, (float)0.588235294, (float)0.588235294, (float)0.411764706, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.5019608, (float)0.5019608, (float)0.5019608, 0, 128, 128, 128, 0, (float)0.498039186000824, 0, 0, 0, (float)0.498039216, (float)0.498039216, (float)0.498039216, (float)0.501960784, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.6627451, (float)0.6627451, (float)0.6627451, 0, 169, 169, 169, 0, (float)0.3372548818588257, 0, 0, 0, (float)0.337254902, (float)0.337254902, (float)0.337254902, (float)0.662745098, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.7529412, (float)0.7529412, (float)0.7529412, 0, 192, 192, 192, 0, (float)0.24705880880355835, 0, 0, 0, (float)0.247058824, (float)0.247058824, (float)0.247058824, (float)0.752941176, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.827451, (float)0.827451, (float)0.827451, 0, 211, 211, 211, 0, (float)0.17254900932312012, 0, 0, 0, (float)0.17254902, (float)0.17254902, (float)0.17254902, (float)0.82745098, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.8627451, (float)0.8627451, (float)0.8627451, 0, 220, 220, 220, 0, (float)0.13725489377975464, 0, 0, 0, (float)0.137254902, (float)0.137254902, (float)0.137254902, (float)0.862745098, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9607843, (float)0.9607843, (float)0.9607843, 0, 245, 245, 245, 0, (float)0.039215683937072754, 0, 0, 0, (float)0.039215686, (float)0.039215686, (float)0.039215686, (float)0.960784314, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, 1, 1, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 1, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.5019608, 0, 0, (float)0.27450982, 128, 0, 0, 70, (float)0.498039186000824, 0, 1, 1, (float)0.498039216, 1, 1, (float)0.167320261, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.54509807, 0, 0, (float)0.67058825, 139, 0, 0, 171, (float)0.45490193367004395, 0, 1, 1, (float)0.454901961, 1, 1, (float)0.181699346, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.64705884, (float)0.16470589, (float)0.16470589, (float)0.1764706, 165, 42, 42, 45, (float)0.3529411554336548, 0, (float)0.7454545689023223, (float)0.7454545689023223, (float)0.352941176, (float)0.835294118, (float)0.835294118, (float)0.325490196, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.69803923, (float)0.13333334, (float)0.13333334, (float)0.4862745, 178, 34, 34, 124, (float)0.3019607663154602, 0, (float)0.80898878035514, (float)0.80898878035514, (float)0.301960784, (float)0.866666667, (float)0.866666667, (float)0.321568627, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.8627451, (float)0.078431375, (float)0.23529412, (float)0.64705884, 220, 20, 60, 165, (float)0.13725489377975464, 0, (float)0.9090909153715637, (float)0.7272727461146913, (float)0.137254902, (float)0.921568627, (float)0.764705882, (float)0.392156863, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(1, 0, 0, (float)0.95686275, 255, 0, 0, 244, 0, 0, 1, 1, 0, 1, 1, (float)0.333333333, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(1, (float)0.3882353, (float)0.2784314, (float)0.5882353, 255, 99, 71, 150, 0, 0, (float)0.611764669418335, (float)0.7215685844421387, 0, (float)0.611764706, (float)0.721568627, (float)0.555555556, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.49803922, (float)0.3137255, (float)0.65882355, 255, 127, 80, 168, 0, 0, (float)0.5019607543945312, (float)0.686274528503418, 0, (float)0.501960784, (float)0.68627451, (float)0.603921569, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.8039216, (float)0.36078432, (float)0.36078432, (float)0.827451, 205, 92, 92, 211, (float)0.19607841968536377, 0, (float)0.5512195454687673, (float)0.5512195454687673, (float)0.196078431, (float)0.639215686, (float)0.639215686, (float)0.508496732, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.9411765, (float)0.5019608, (float)0.5019608, (float)0.37254903, 240, 128, 128, 95, (float)0.05882352590560913, 0, (float)0.46666663711269707, (float)0.46666663711269707, (float)0.058823529, (float)0.498039216, (float)0.498039216, (float)0.648366013, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.9137255, (float)0.5882353, (float)0.47843137, (float)0.23529412, 233, 150, 122, 60, (float)0.08627450466156006, 0, (float)0.35622315272831884, (float)0.47639488394150725, (float)0.08627451, (float)0.411764706, (float)0.521568627, (float)0.660130719, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.98039216, (float)0.5019608, (float)0.44705883, (float)0.8862745, 250, 128, 114, 226, (float)0.019607841968536377, 0, (float)0.4879999703311921, (float)0.5440000277233124, (float)0.019607843, (float)0.498039216, (float)0.552941176, (float)0.643137255, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.627451, (float)0.47843137, (float)0.4745098, 255, 160, 122, 121, 0, 0, (float)0.37254899740219116, (float)0.5215686559677124, 0, (float)0.37254902, (float)0.521568627, (float)0.701960784, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.27058825, 0, (float)0.7372549, 255, 69, 0, 188, 0, 0, (float)0.7294117212295532, 1, 0, (float)0.729411765, 1, (float)0.423529412, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(1, (float)0.54901963, 0, (float)0.34901962, 255, 140, 0, 89, 0, 0, (float)0.45098036527633667, 1, 0, (float)0.450980392, 1, (float)0.516339869, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.64705884, 0, (float)0.42745098, 255, 165, 0, 109, 0, 0, (float)0.3529411554336548, 1, 0, (float)0.352941176, 1, (float)0.549019608, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.84313726, 0, (float)0.7411765, 255, 215, 0, 189, 0, 0, (float)0.15686273574829102, 1, 0, (float)0.156862745, 1, (float)0.614379085, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.72156864, (float)0.5254902, (float)0.043137256, (float)0.8, 184, 134, 11, 204, (float)0.27843135595321655, 0, (float)0.2717391079879725, (float)0.940217396242646, (float)0.278431373, (float)0.474509804, (float)0.956862745, (float)0.430065359, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.85490197, (float)0.64705884, (float)0.1254902, (float)0.5176471, 218, 165, 32, 132, (float)0.1450980305671692, 0, (float)0.24311924910452054, (float)0.8532110194085913, (float)0.145098039, (float)0.352941176, (float)0.874509804, (float)0.54248366, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.93333334, (float)0.9098039, (float)0.6666667, (float)0.91764706, 238, 232, 170, 234, (float)0.06666666269302368, 0, (float)0.025210082423644056, (float)0.28571426746796597, (float)0.066666667, (float)0.090196078, (float)0.333333333, (float)0.836601307, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.7411765, (float)0.7176471, (float)0.41960785, (float)0.09803922, 189, 183, 107, 25, (float)0.2588235139846802, 0, (float)0.03174602919304878, (float)0.4338623989716666, (float)0.258823529, (float)0.282352941, (float)0.580392157, (float)0.626143791, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.9411765, (float)0.9019608, (float)0.54901963, (float)0.5921569, 240, 230, 140, 151, (float)0.05882352590560913, 0, (float)0.04166666402791938, (float)0.4166666402791938, (float)0.058823529, (float)0.098039216, (float)0.450980392, (float)0.797385621, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.5019608, (float)0.5019608, 0, (float)0.972549, 128, 128, 0, 248, (float)0.498039186000824, 0, 0, 1, (float)0.498039216, (float)0.498039216, 1, (float)0.334640523, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(1, 1, 0, (float)0.3372549, 255, 255, 0, 86, 0, 0, 0, 1, 0, 0, 1, (float)0.666666667, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.6039216, (float)0.8039216, (float)0.19607843, (float)0.96862745, 154, 205, 50, 247, (float)0.19607841968536377, (float)0.24878046935970508, 0, (float)0.7560975790591127, (float)0.396078431, (float)0.196078431, (float)0.803921569, (float)0.534640523, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.33333334, (float)0.41960785, (float)0.18431373, (float)0.34901962, 85, 107, 47, 89, (float)0.5803921222686768, (float)0.20560744742929254, 0, (float)0.5607477259465113, (float)0.666666667, (float)0.580392157, (float)0.815686275, (float)0.3124183, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.41960785, (float)0.5568628, (float)0.13725491, (float)0.050980393, 107, 142, 35, 13, (float)0.4431372284889221, (float)0.2464788468571995, 0, (float)0.7535211531428005, (float)0.580392157, (float)0.443137255, (float)0.862745098, (float)0.37124183, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.4862745, (float)0.9882353, 0, (float)0.98039216, 124, 252, 0, 250, (float)0.011764705181121826, (float)0.5079365376149355, 0, 1, (float)0.51372549, (float)0.011764706, 1, (float)0.491503268, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.49803922, 1, 0, (float)0.7529412, 127, 255, 0, 192, 0, (float)0.5019607543945312, 0, 1, (float)0.501960784, 0, 1, (float)0.499346405, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.6784314, 1, (float)0.18431373, (float)0.21960784, 173, 255, 47, 56, 0, (float)0.3215686082839966, 0, (float)0.8156862854957581, (float)0.321568627, 0, (float)0.815686275, (float)0.620915033, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(0, (float)0.39215687, 0, (float)0.14901961, 0, 100, 0, 38, (float)0.6078431606292725, 1, 0, 1, 1, (float)0.607843137, 1, (float)0.130718954, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, (float)0.5019608, 0, (float)0.41568628, 0, 128, 0, 106, (float)0.498039186000824, 1, 0, 1, 1, (float)0.498039216, 1, (float)0.167320261, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.13333334, (float)0.54509807, (float)0.13333334, (float)0.36078432, 34, 139, 34, 92, (float)0.45490193367004395, (float)0.7553957101998988, 0, (float)0.7553957101998988, (float)0.866666667, (float)0.454901961, (float)0.866666667, (float)0.270588235, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, 1, 0, (float)0.30588236, 0, 255, 0, 78, 0, 1, 0, 1, 1, 0, 1, (float)0.333333333, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.19607843, (float)0.8039216, (float)0.19607843, (float)0.5568628, 50, 205, 50, 142, (float)0.19607841968536377, (float)0.7560975790591127, 0, (float)0.7560975790591127, (float)0.803921569, (float)0.196078431, (float)0.803921569, (float)0.39869281, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.5647059, (float)0.93333334, (float)0.5647059, (float)0.28235295, 144, 238, 144, 72, (float)0.06666666269302368, (float)0.39495795797042355, 0, (float)0.39495795797042355, (float)0.435294118, (float)0.066666667, (float)0.435294118, (float)0.687581699, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.59607846, (float)0.9843137, (float)0.59607846, (float)0.67058825, 152, 251, 152, 171, (float)0.0156862735748291, (float)0.3944222868729186, 0, (float)0.3944222868729186, (float)0.403921569, (float)0.015686275, (float)0.403921569, (float)0.725490196, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.56078434, (float)0.7372549, (float)0.56078434, (float)0.23529412, 143, 188, 143, 60, (float)0.26274508237838745, (float)0.23936168277605013, 0, (float)0.23936168277605013, (float)0.439215686, (float)0.262745098, (float)0.439215686, (float)0.619607843, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(0, (float)0.98039216, (float)0.6039216, (float)0.81960785, 0, 250, 154, 209, (float)0.019607841968536377, 1, 0, (float)0.3839999766540528, 1, (float)0.019607843, (float)0.396078431, (float)0.528104575, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(0, 1, (float)0.49803922, (float)0.8, 0, 255, 127, 204, 0, 1, 0, (float)0.5019607543945312, 1, 0, (float)0.501960784, (float)0.499346405, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.18039216, (float)0.54509807, (float)0.34117648, (float)0.7647059, 46, 139, 87, 195, (float)0.45490193367004395, (float)0.6690647843880984, 0, (float)0.3741006785178019, (float)0.819607843, (float)0.454901961, (float)0.658823529, (float)0.355555556, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.4, (float)0.8039216, (float)0.6666667, (float)0.40392157, 102, 205, 170, 103, (float)0.19607841968536377, (float)0.5024390612805898, 0, (float)0.17073169465862112, (float)0.6, (float)0.196078431, (float)0.333333333, (float)0.623529412, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.23529412, (float)0.7019608, (float)0.44313726, (float)0.49019608, 60, 179, 113, 125, (float)0.29803919792175293, (float)0.6648044977357461, 0, (float)0.36871505249067926, (float)0.764705882, (float)0.298039216, (float)0.556862745, (float)0.460130719, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.1254902, (float)0.69803923, (float)0.6666667, (float)0.6313726, 32, 178, 170, 161, (float)0.3019607663154602, (float)0.8202247344518965, 0, (float)0.044943816387025874, (float)0.874509804, (float)0.301960784, (float)0.333333333, (float)0.496732026, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.18431373, (float)0.30980393, (float)0.30980393, (float)0.90588236, 47, 79, 79, 231, (float)0.6901960372924805, (float)0.4050634056019184, 0, 0, (float)0.815686275, (float)0.690196078, (float)0.690196078, (float)0.267973856, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, (float)0.5019608, (float)0.5019608, (float)0.5529412, 0, 128, 128, 141, (float)0.498039186000824, 1, 0, 0, 1, (float)0.498039216, (float)0.498039216, (float)0.334640523, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, (float)0.54509807, (float)0.54509807, (float)0.7411765, 0, 139, 139, 189, (float)0.45490193367004395, 1, 0, 0, 1, (float)0.454901961, (float)0.454901961, (float)0.363398693, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, 1, 1, (float)0.87058824, 0, 255, 255, 222, 0, 1, 0, 0, 1, 0, 0, (float)0.666666667, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(0, 1, 1, (float)0.6784314, 0, 255, 255, 173, 0, 1, 0, 0, 1, 0, 0, (float)0.666666667, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.8784314, 1, 1, (float)0.4509804, 224, 255, 255, 115, 0, (float)0.12156862020492554, 0, 0, (float)0.121568627, 0, 0, (float)0.959477124, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(0, (float)0.80784315, (float)0.81960785, (float)0.2901961, 0, 206, 209, 74, (float)0.18039214611053467, 1, (float)0.014354065941769816, 0, 1, (float)0.192156863, (float)0.180392157, (float)0.54248366, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.2509804, (float)0.8784314, (float)0.8156863, (float)0.10980392, 64, 224, 208, 28, (float)0.12156862020492554, (float)0.714285733672429, 0, (float)0.07142856658189277, (float)0.749019608, (float)0.121568627, (float)0.184313725, (float)0.648366013, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.28235295, (float)0.81960785, (float)0.8, (float)0.09019608, 72, 209, 204, 23, (float)0.18039214611053467, (float)0.6555024173975245, 0, (float)0.023923443236283027, (float)0.717647059, (float)0.180392157, (float)0.2, (float)0.633986928, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.6862745, (float)0.93333334, (float)0.93333334, (float)0.27450982, 175, 238, 238, 70, (float)0.06666666269302368, (float)0.2647058654482626, 0, 0, (float)0.31372549, (float)0.066666667, (float)0.066666667, (float)0.850980392, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.49803922, 1, (float)0.83137256, (float)0.10980392, 127, 255, 212, 28, 0, (float)0.5019607543945312, 0, (float)0.16862744092941284, (float)0.501960784, 0, (float)0.168627451, (float)0.776470588, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.6901961, (float)0.8784314, (float)0.9019608, (float)0.8156863, 176, 224, 230, 208, (float)0.09803920984268188, (float)0.23478259318041678, (float)0.026086954797824084, 0, (float)0.309803922, (float)0.121568627, (float)0.098039216, (float)0.823529412, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.37254903, (float)0.61960787, (float)0.627451, (float)0.6392157, 95, 158, 160, 163, (float)0.37254899740219116, (float)0.4062499614083222, (float)0.01249999881256376, 0, (float)0.62745098, (float)0.380392157, (float)0.37254902, (float)0.539869281, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.27450982, (float)0.50980395, (float)0.7058824, (float)0.7764706, 70, 130, 180, 198, (float)0.29411762952804565, (float)0.6111111439488545, (float)0.27777775432224683, 0, (float)0.725490196, (float)0.490196078, (float)0.294117647, (float)0.496732026, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.39215687, (float)0.58431375, (float)0.92941177, (float)0.7607843, 100, 149, 237, 194, (float)0.07058823108673096, (float)0.578059098789696, (float)0.3713079930650675, 0, (float)0.607843137, (float)0.415686275, (float)0.070588235, (float)0.635294118, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(0, (float)0.7490196, 1, (float)0.64705884, 0, 191, 255, 165, 0, 1, (float)0.2509803771972656, 0, 1, (float)0.250980392, 0, (float)0.583006536, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.11764706, (float)0.5647059, 1, (float)0.35686275, 30, 144, 255, 91, 0, (float)0.8823529481887817, (float)0.43529409170150757, 0, (float)0.882352941, (float)0.435294118, 0, (float)0.560784314, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.6784314, (float)0.84705883, (float)0.9019608, (float)0.70980394, 173, 216, 230, 181, (float)0.09803920984268188, (float)0.24782607057932882, (float)0.060869561194922865, 0, (float)0.321568627, (float)0.152941176, (float)0.098039216, (float)0.809150327, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.5294118, (float)0.80784315, (float)0.92156863, (float)0.34117648, 135, 206, 235, 87, (float)0.07843136787414551, (float)0.4255318873713276, (float)0.123404247337685, 0, (float)0.470588235, (float)0.192156863, (float)0.078431373, (float)0.752941176, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.5294118, (float)0.80784315, (float)0.98039216, (float)0.5568628, 135, 206, 250, 142, (float)0.019607841968536377, (float)0.4599999720335007, (float)0.17599998929977417, 0, (float)0.470588235, (float)0.192156863, (float)0.019607843, (float)0.77254902, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.09803922, (float)0.09803922, (float)0.4392157, (float)0.9764706, 25, 25, 112, 249, (float)0.5607843399047852, (float)0.7767857142857143, (float)0.7767857142857143, 0, (float)0.901960784, (float)0.901960784, (float)0.560784314, (float)0.211764706, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, 0, (float)0.5019608, (float)0.9529412, 0, 0, 128, 243, (float)0.498039186000824, 1, 1, 0, 1, 1, (float)0.498039216, (float)0.167320261, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, 0, (float)0.54509807, (float)0.54509807, 0, 0, 139, 139, (float)0.45490193367004395, 1, 1, 0, 1, 1, (float)0.454901961, (float)0.181699346, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, 0, (float)0.8039216, (float)0.43137255, 0, 0, 205, 110, (float)0.19607841968536377, 1, 1, 0, 1, 1, (float)0.196078431, (float)0.267973856, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition(0, 0, 1, (float)0.9137255, 0, 0, 255, 233, 0, 1, 1, 0, 1, 1, 0, (float)0.333333333, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.25490198, (float)0.4117647, (float)0.88235295, (float)0.2784314, 65, 105, 225, 71, (float)0.11764705181121826, (float)0.7111110630741829, (float)0.5333332973056373, 0, (float)0.745098039, (float)0.588235294, (float)0.117647059, (float)0.516339869, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.5411765, (float)0.16862746, (float)0.8862745, (float)0.5803922, 138, 43, 226, 148, (float)0.11372548341751099, (float)0.3893805047864316, (float)0.8097345260702664, 0, (float)0.458823529, (float)0.831372549, (float)0.11372549, (float)0.532026144, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.29411766, 0, (float)0.50980395, (float)0.4745098, 75, 0, 130, 121, (float)0.4901960492134094, (float)0.42307687361212454, 1, 0, (float)0.705882353, 1, (float)0.490196078, (float)0.267973856, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.28235295, (float)0.23921569, (float)0.54509807, (float)0.42745098, 72, 61, 139, 109, (float)0.45490193367004395, (float)0.48201444512919744, (float)0.5611511271233478, 0, (float)0.717647059, (float)0.760784314, (float)0.454901961, (float)0.355555555, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.41568628, (float)0.3529412, (float)0.8039216, (float)0.6392157, 106, 90, 205, 163, (float)0.19607841968536377, (float)0.48292686760531883, (float)0.5609756423064028, 0, (float)0.584313725, (float)0.647058824, (float)0.196078431, (float)0.524183007, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.48235294, (float)0.40784314, (float)0.93333334, (float)0.23921569, 123, 104, 238, 61, (float)0.06666666269302368, (float)0.48319324645317774, (float)0.5630252379901697, 0, (float)0.517647059, (float)0.592156863, (float)0.066666667, (float)0.607843137, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.5764706, (float)0.4392157, (float)0.85882354, (float)0.6, 147, 112, 219, 153, (float)0.14117646217346191, (float)0.32876710047035573, (float)0.4885845103794466, 0, (float)0.423529412, (float)0.560784314, (float)0.141176471, (float)0.624836601, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.54509807, 0, (float)0.54509807, (float)0.23529412, 139, 0, 139, 60, (float)0.45490193367004395, 0, 1, 0, (float)0.454901961, 1, (float)0.454901961, (float)0.363398693, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.5803922, 0, (float)0.827451, (float)0.043137256, 148, 0, 211, 11, (float)0.17254900932312012, (float)0.29857817754433624, 1, 0, (float)0.419607843, 1, (float)0.17254902, (float)0.469281046, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.6, (float)0.19607843, (float)0.8, (float)0.9607843, 153, 50, 204, 245, (float)0.19999998807907104, (float)0.24999998137354879, (float)0.7549019790455405, 0, (float)0.4, (float)0.803921569, (float)0.2, (float)0.532026144, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.7294118, (float)0.33333334, (float)0.827451, (float)0.39215687, 186, 85, 211, 100, (float)0.17254900932312012, (float)0.11848340378743502, (float)0.5971563550886725, 0, (float)0.270588235, (float)0.666666667, (float)0.17254902, (float)0.630065359, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.5019608, 0, (float)0.5019608, (float)0.65882355, 128, 0, 128, 168, (float)0.498039186000824, 0, 1, 0, (float)0.498039216, 1, (float)0.498039216, (float)0.334640523, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.84705883, (float)0.7490196, (float)0.84705883, (float)0.27058825, 216, 191, 216, 69, (float)0.15294116735458374, 0, (float)0.11574073259645905, 0, (float)0.152941176, (float)0.250980392, (float)0.152941176, (float)0.814379085, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.8666667, (float)0.627451, (float)0.8666667, (float)0.22745098, 221, 160, 221, 58, (float)0.13333332538604736, 0, (float)0.2760180805644798, 0, (float)0.133333333, (float)0.37254902, (float)0.133333333, (float)0.786928105, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.93333334, (float)0.50980395, (float)0.93333334, (float)0.17254902, 238, 130, 238, 44, (float)0.06666666269302368, 0, (float)0.453781483625593, 0, (float)0.066666667, (float)0.490196078, (float)0.066666667, (float)0.792156863, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, 0, 1, (float)0.5058824, 255, 0, 255, 129, 0, 0, 1, 0, 0, 1, 0, (float)0.666666667, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.85490197, (float)0.4392157, (float)0.8392157, (float)0.32941177, 218, 112, 214, 84, (float)0.1450980305671692, 0, (float)0.4862385679300698, (float)0.01834862257392608, (float)0.145098039, (float)0.560784314, (float)0.160784314, (float)0.711111111, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.78039217, (float)0.08235294, (float)0.52156866, (float)0.90588236, 199, 21, 133, 231, (float)0.21960783004760742, 0, (float)0.8944723698690151, (float)0.3316582661259525, (float)0.219607843, (float)0.917647059, (float)0.478431373, (float)0.461437908, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.85882354, (float)0.4392157, (float)0.5764706, (float)0.67058825, 219, 112, 147, 171, (float)0.14117646217346191, 0, (float)0.4885845103794466, (float)0.32876710047035573, (float)0.141176471, (float)0.560784314, (float)0.423529412, (float)0.624836601, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.078431375, (float)0.5764706, (float)0.43137255, 255, 20, 147, 110, 0, 0, (float)0.9215686321258545, (float)0.42352938652038574, 0, (float)0.921568627, (float)0.423529412, (float)0.551633987, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.4117647, (float)0.7058824, (float)0.41568628, 255, 105, 180, 106, 0, 0, (float)0.5882352590560913, (float)0.29411762952804565, 0, (float)0.588235294, (float)0.294117647, (float)0.705882353, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.7137255, (float)0.75686276, (float)0.8509804, 255, 182, 193, 217, 0, 0, (float)0.2862744927406311, (float)0.24313724040985107, 0, (float)0.28627451, (float)0.243137255, (float)0.823529412, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.7529412, (float)0.79607844, (float)0.03529412, 255, 192, 203, 9, 0, 0, (float)0.24705880880355835, (float)0.20392155647277832, 0, (float)0.247058824, (float)0.203921569, (float)0.849673202, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.98039216, (float)0.92156863, (float)0.84313726, (float)0.34901962, 250, 235, 215, 89, (float)0.019607841968536377, 0, (float)0.059999996352195745, (float)0.13999999148845674, (float)0.019607843, (float)0.078431373, (float)0.156862745, (float)0.91503268, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9607843, (float)0.9607843, (float)0.8627451, (float)0.08627451, 245, 245, 220, 22, (float)0.039215683937072754, 0, 0, (float)0.10204080999617476, (float)0.039215686, (float)0.039215686, (float)0.137254902, (float)0.928104575, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.89411765, (float)0.76862746, (float)0.007843138, 255, 228, 196, 2, 0, 0, (float)0.10588234663009644, (float)0.23137253522872925, 0, (float)0.105882353, (float)0.231372549, (float)0.887581699, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.92156863, (float)0.8039216, (float)0.54509807, 255, 235, 205, 139, 0, 0, (float)0.07843136787414551, (float)0.19607841968536377, 0, (float)0.078431373, (float)0.196078431, (float)0.908496732, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9607843, (float)0.87058824, (float)0.7019608, (float)0.91764706, 245, 222, 179, 234, (float)0.039215683937072754, 0, (float)0.09387754519648078, (float)0.2693877383899014, (float)0.039215686, (float)0.129411765, (float)0.298039216, (float)0.844444444, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.972549, (float)0.8627451, (float)0.69803923, 255, 248, 220, 178, 0, 0, (float)0.027450978755950928, (float)0.13725489377975464, 0, (float)0.02745098, (float)0.137254902, (float)0.945098039, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.98039216, (float)0.8039216, (float)0.02745098, 255, 250, 205, 7, 0, 0, (float)0.019607841968536377, (float)0.19607841968536377, 0, (float)0.019607843, (float)0.196078431, (float)0.928104575, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.98039216, (float)0.98039216, (float)0.8235294, (float)0.47843137, 250, 250, 210, 122, (float)0.019607841968536377, 0, 0, (float)0.15999999027252199, (float)0.019607843, (float)0.019607843, (float)0.176470588, (float)0.928104575, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, 1, (float)0.8784314, (float)0.76862746, 255, 255, 224, 196, 0, 0, 0, (float)0.12156862020492554, 0, 0, (float)0.121568627, (float)0.959477124, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.54509807, (float)0.27058825, (float)0.07450981, (float)0.22745098, 139, 69, 19, 58, (float)0.45490193367004395, 0, (float)0.5035970672355025, (float)0.8633093674646494, (float)0.454901961, (float)0.729411765, (float)0.925490196, (float)0.296732026, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.627451, (float)0.32156864, (float)0.1764706, (float)0.8117647, 160, 82, 45, 207, (float)0.37254899740219116, 0, (float)0.4875000486848859, (float)0.7187500267173154, (float)0.37254902, (float)0.678431373, (float)0.823529412, (float)0.375163398, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.8235294, (float)0.4117647, (float)0.11764706, (float)0.6313726, 210, 105, 30, 161, (float)0.1764705777168274, 0, (float)0.4999999638114661, (float)0.8571428674824383, (float)0.176470588, (float)0.588235294, (float)0.882352941, (float)0.450980392, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.8039216, (float)0.52156866, (float)0.24705882, (float)0.972549, 205, 133, 63, 248, (float)0.19607841968536377, 0, (float)0.35121948615487775, (float)0.692682949614482, (float)0.196078431, (float)0.478431373, (float)0.752941176, (float)0.524183007, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.95686275, (float)0.6431373, (float)0.3764706, 1, 244, 164, 96, 255, (float)0.04313725233078003, 0, (float)0.32786883203549533, (float)0.6065574015574056, (float)0.043137255, (float)0.356862745, (float)0.623529412, (float)0.658823529, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.87058824, (float)0.72156864, (float)0.5294118, (float)0.7019608, 222, 184, 135, 179, (float)0.1294117569923401, 0, (float)0.1711711594519722, (float)0.3918918650610942, (float)0.129411765, (float)0.278431373, (float)0.470588235, (float)0.707189542, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.8235294, (float)0.7058824, (float)0.54901963, (float)0.22745098, 210, 180, 140, 58, (float)0.1764705777168274, 0, (float)0.14285713251756174, (float)0.3333333092076441, (float)0.176470588, (float)0.294117647, (float)0.450980392, (float)0.692810458, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.7372549, (float)0.56078434, (float)0.56078434, (float)0.101960786, 188, 143, 143, 26, (float)0.26274508237838745, 0, (float)0.23936168277605013, (float)0.23936168277605013, (float)0.262745098, (float)0.439215686, (float)0.439215686, (float)0.619607843, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition(1, (float)0.89411765, (float)0.70980394, (float)0.3254902, 255, 228, 181, 83, 0, 0, (float)0.10588234663009644, (float)0.2901960611343384, 0, (float)0.105882353, (float)0.290196078, (float)0.867973856, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.87058824, (float)0.6784314, (float)0.18039216, 255, 222, 173, 46, 0, 0, (float)0.1294117569923401, (float)0.3215686082839966, 0, (float)0.129411765, (float)0.321568627, (float)0.849673203, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.85490197, (float)0.7254902, (float)0.98039216, 255, 218, 185, 250, 0, 0, (float)0.1450980305671692, (float)0.2745097875595093, 0, (float)0.145098039, (float)0.274509804, (float)0.860130719, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.89411765, (float)0.88235295, (float)0.20392157, 255, 228, 225, 52, 0, 0, (float)0.10588234663009644, (float)0.11764705181121826, 0, (float)0.105882353, (float)0.117647059, (float)0.925490196, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.9411765, (float)0.9607843, (float)0.16470589, 255, 240, 245, 42, 0, 0, (float)0.05882352590560913, (float)0.039215683937072754, 0, (float)0.058823529, (float)0.039215686, (float)0.967320262, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.98039216, (float)0.9411765, (float)0.9019608, (float)0.45490196, 250, 240, 230, 116, (float)0.019607841968536377, 0, (float)0.039999997568130496, (float)0.07999999513626099, (float)0.019607843, (float)0.058823529, (float)0.098039216, (float)0.941176471, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.99215686, (float)0.9607843, (float)0.9019608, (float)0.7607843, 253, 245, 230, 194, (float)0.00784313678741455, 0, (float)0.03162055146005288, (float)0.09090908544765203, (float)0.007843137, (float)0.039215686, (float)0.098039216, (float)0.951633987, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.9372549, (float)0.8352941, (float)0.77254903, 255, 239, 213, 197, 0, 0, (float)0.0627450942993164, (float)0.16470587253570557, 0, (float)0.062745098, (float)0.164705882, (float)0.924183007, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.9607843, (float)0.93333334, (float)0.7294118, 255, 245, 238, 186, 0, 0, (float)0.039215683937072754, (float)0.06666666269302368, 0, (float)0.039215686, (float)0.066666667, (float)0.964705882, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9607843, 1, (float)0.98039216, (float)0.2901961, 245, 255, 250, 74, 0, (float)0.039215683937072754, 0, (float)0.019607841968536377, (float)0.039215686, 0, (float)0.019607843, (float)0.980392157, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.4392157, (float)0.5019608, (float)0.5647059, (float)0.16470589, 112, 128, 144, 42, (float)0.43529409170150757, (float)0.22222230431657874, (float)0.1111110993833459, 0, (float)0.560784314, (float)0.498039216, (float)0.435294118, (float)0.501960784, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.46666667, (float)0.53333336, (float)0.6, (float)0.043137256, 119, 136, 153, 11, (float)0.3999999761581421, (float)0.22222220014642874, (float)0.11111110007321437, 0, (float)0.533333333, (float)0.466666667, (float)0.4, (float)0.533333333, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.6901961, (float)0.76862746, (float)0.87058824, (float)0.18039216, 176, 196, 222, 46, (float)0.1294117569923401, (float)0.20720719302080845, (float)0.11711710909871781, 0, (float)0.309803922, (float)0.231372549, (float)0.129411765, (float)0.776470588, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9019608, (float)0.9019608, (float)0.98039216, (float)0.94509804, 230, 230, 250, 241, (float)0.019607841968536377, (float)0.07999999513626099, (float)0.07999999513626099, 0, (float)0.098039216, (float)0.098039216, (float)0.019607843, (float)0.928104575, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.98039216, (float)0.9411765, (float)0.24313726, 255, 250, 240, 62, 0, 0, (float)0.019607841968536377, (float)0.05882352590560913, 0, (float)0.019607843, (float)0.058823529, (float)0.973856209, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9411765, (float)0.972549, 1, (float)0.77254903, 240, 248, 255, 197, 0, (float)0.05882352590560913, (float)0.027450978755950928, 0, (float)0.058823529, (float)0.02745098, 0, (float)0.97124183, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.972549, (float)0.972549, 1, (float)0.84313726, 248, 248, 255, 215, 0, (float)0.027450978755950928, (float)0.027450978755950928, 0, (float)0.02745098, (float)0.02745098, 0, (float)0.981699347, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9411765, 1, (float)0.9411765, (float)0.73333335, 240, 255, 240, 187, 0, (float)0.05882352590560913, 0, (float)0.05882352590560913, (float)0.058823529, 0, (float)0.058823529, (float)0.960784314, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, 1, (float)0.9411765, (float)0.67058825, 255, 255, 240, 171, 0, 0, 0, (float)0.05882352590560913, 0, 0, (float)0.058823529, (float)0.980392157, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9411765, 1, 1, (float)0.8509804, 240, 255, 255, 217, 0, (float)0.05882352590560913, 0, 0, (float)0.058823529, 0, 0, (float)0.980392157, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, (float)0.98039216, (float)0.98039216, (float)0.34901962, 255, 250, 250, 89, 0, 0, (float)0.019607841968536377, (float)0.019607841968536377, 0, (float)0.019607843, (float)0.019607843, (float)0.986928105, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(0, 0, 0, (float)0.1882353, 0, 0, 0, 48, 1, 0, 0, 0, 1, 1, 1, 0, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.4117647, (float)0.4117647, (float)0.4117647, (float)0.27450982, 105, 105, 105, 70, (float)0.5882352590560913, 0, 0, 0, (float)0.588235294, (float)0.588235294, (float)0.588235294, (float)0.411764706, true, true, Colors.Black, Colors.White)],
		[new ColorTestDefinition((float)0.5019608, (float)0.5019608, (float)0.5019608, (float)0.34509805, 128, 128, 128, 88, (float)0.498039186000824, 0, 0, 0, (float)0.498039216, (float)0.498039216, (float)0.498039216, (float)0.501960784, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.6627451, (float)0.6627451, (float)0.6627451, (float)0.2784314, 169, 169, 169, 71, (float)0.3372548818588257, 0, 0, 0, (float)0.337254902, (float)0.337254902, (float)0.337254902, (float)0.662745098, false, true, Colors.White, Colors.White)],
		[new ColorTestDefinition((float)0.7529412, (float)0.7529412, (float)0.7529412, (float)0.105882354, 192, 192, 192, 27, (float)0.24705880880355835, 0, 0, 0, (float)0.247058824, (float)0.247058824, (float)0.247058824, (float)0.752941176, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.827451, (float)0.827451, (float)0.827451, (float)0.9882353, 211, 211, 211, 252, (float)0.17254900932312012, 0, 0, 0, (float)0.17254902, (float)0.17254902, (float)0.17254902, (float)0.82745098, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.8627451, (float)0.8627451, (float)0.8627451, (float)0.627451, 220, 220, 220, 160, (float)0.13725489377975464, 0, 0, 0, (float)0.137254902, (float)0.137254902, (float)0.137254902, (float)0.862745098, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition((float)0.9607843, (float)0.9607843, (float)0.9607843, (float)0.69411767, 245, 245, 245, 177, (float)0.039215683937072754, 0, 0, 0, (float)0.039215686, (float)0.039215686, (float)0.039215686, (float)0.960784314, false, false, Colors.White, Colors.Black)],
		[new ColorTestDefinition(1, 1, 1, (float)0.84705883, 255, 255, 255, 216, 0, 0, 0, 0, 0, 0, 0, 1, false, false, Colors.White, Colors.Black)],
	];

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void ToRgbString(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToRgbString();

		Assert.Equal(testDef.ExpectedRgb, result);
	}

	[Fact]
	public void ToRgbStringNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.ToRgbString(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void ToRgbaString(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToRgbaString();

		Assert.Equal(testDef.ExpectedRgba, result);
	}

	[Fact]
	public void ToRgbaStringNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.ToRgbaString(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void ToCmykString(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToCmykString();

		Assert.Equal(testDef.ExpectedCmyk, result);
	}

	[Fact]
	public void ToCmykStringNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.ToCmykString(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void ToCmykaString(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToCmykaString();

		Assert.Equal(testDef.ExpectedCmyka, result);
	}

	[Fact]
	public void ToCmykaStringNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.ToCmykaString(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void ToHslString(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToHslString();

		Assert.Equal(testDef.ExpectedHslString, result);
	}

	[Fact]
	public void ToHslStringNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.ToHslString(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void ToHslaString(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToHslaString();

		Assert.Equal(testDef.ExpectedHslaString, result);
	}

	[Fact]
	public void ToHslaStringNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.ToHslaString(null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void GetByteRed(ColorTestDefinition testDef)
	{
		var result = testDef.Color.GetByteRed();

		Assert.Equal(testDef.ExpectedByteR, result);
	}

	[Fact]
	public void GetByteRedNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.GetByteRed(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void GetByteGreen(ColorTestDefinition testDef)
	{
		var result = testDef.Color.GetByteGreen();

		Assert.Equal(testDef.ExpectedByteG, result);
	}

	[Fact]
	public void GetByteGreenNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.GetByteGreen(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void GetByteBlue(ColorTestDefinition testDef)
	{
		var result = testDef.Color.GetByteBlue();

		Assert.Equal(testDef.ExpectedByteB, result);
	}

	[Fact]
	public void GetByteBlueNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.GetByteBlue(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void GetByteAlpha(ColorTestDefinition testDef)
	{
		var result = testDef.Color.GetByteAlpha();

		Assert.Equal(testDef.ExpectedByteA, result);
	}

	[Fact]
	public void GetByteAlphaNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.GetByteAlpha(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void GetPctBlackKey(ColorTestDefinition def)
	{
		var result = def.Color.GetPercentBlackKey();

		Assert.Equal(def.ExpectedPctBlack, result);
	}

	[Fact]
	public void GetPercentBlackKeyNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.GetPercentBlackKey(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void GetDegreeHue(ColorTestDefinition def)
	{
		var result = def.Color.GetDegreeHue();

		Assert.Equal(def.ExpectedDegreeHue, result);
	}

	[Fact]
	public void GetDegreeHueNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.GetDegreeHue(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void GetPctCyan(ColorTestDefinition testDef)
	{
		var result = testDef.Color.GetPercentCyan();

		Assert.Equal(testDef.ExpectedPctCyan, result);
	}

	[Fact]
	public void GetPercentCyanNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.GetPercentCyan(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void GetPctMagenta(ColorTestDefinition testDef)
	{
		var result = testDef.Color.GetPercentMagenta();

		Assert.Equal(testDef.ExpectedPctMagenta, result);
	}

	[Fact]
	public void GetPercentMagentaNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.GetPercentMagenta(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void GetPctYellow(ColorTestDefinition testDef)
	{
		var result = testDef.Color.GetPercentYellow();

		Assert.Equal(testDef.ExpectedPctYellow, result);
	}

	[Fact]
	public void GetPercentYellowNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.GetPercentYellow(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void ToInverseColor(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToInverseColor();

		Assert.Equal(testDef.ExpectedInverse, result, (color1, color2) =>
		{
			return Math.Abs(color1.Red - color2.Red) < tolerance
				&& Math.Abs(color1.Green - color2.Green) < tolerance
				&& Math.Abs(color1.Blue - color2.Blue) < tolerance
				&& Math.Abs(color1.Alpha - color2.Alpha) < tolerance;
		});
	}

	[Fact]
	public void ToInverseColorNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.ToInverseColor(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void ToGrayScale(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToGrayScale();

		Assert.Equal(testDef.ExpectedGreyScale, result, (color1, color2) =>
		{
			return Math.Abs(color1.Red - color2.Red) < tolerance
				   && Math.Abs(color1.Green - color2.Green) < tolerance
				   && Math.Abs(color1.Blue - color2.Blue) < tolerance
				   && Math.Abs(color1.Alpha - color2.Alpha) < tolerance;
		});
	}

	[Fact]
	public void ToGrayScaleNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.ToGrayScale(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void IsDark(ColorTestDefinition testDef)
	{
		var result = testDef.Color.IsDark();

		Assert.Equal(testDef.ExpectedIsDark, result);
	}

	[Fact]
	public void IsDarkNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.IsDark(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void IsDarkForTheEye(ColorTestDefinition testDef)
	{
		var result = testDef.Color.IsDarkForTheEye();

		Assert.Equal(testDef.ExpectedIsDarkForEye, result);
	}

	[Fact]
	public void IsDarkForTheEyeNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.IsDarkForTheEye(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public void GetPctCyanShouldNotCrashOnBlackColor()
	{
		var black = Colors.Black;
		black.GetPercentCyan();
		Assert.True(true);
	}

	[Fact]
	public void GetPctMagentaShouldNotCrashOnBlackColor()
	{
		var black = Colors.Black;
		black.GetPercentMagenta();
		Assert.True(true);
	}

	[Fact]
	public void GetPctYellowShouldNotCrashOnBlackColor()
	{
		var black = Colors.Black;
		black.GetPercentYellow();
		Assert.True(true);
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void ToBlackOrWhite(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToBlackOrWhite();

		Assert.Equal(testDef.ExpectedToBlackOrWhite, result);
	}

	[Fact]
	public void ToBlackOrWhiteNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.ToBlackOrWhite(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void ToBlackOrWhiteForText(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToBlackOrWhiteForText();

		Assert.Equal(testDef.ExpectedToBlackOrWhiteForText, result);
	}

	[Fact]
	public void ToBlackOrWhiteForTextNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.ToBlackOrWhiteForText(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void WithRed_Double(ColorTestDefinition testDef)
	{
		var red = Random.Shared.NextDouble();
		var result = testDef.Color.WithRed(red);

		Assert.Equal((float)red, result.Red);
	}

	[Fact]
	public void WithRed_Double_RedGeaterThan1ShouldThrowAgumentOutOfRangeException()
	{
		Color c = new();
		var red = Random.Shared.Next(2, int.MaxValue);

		Assert.Throws<ArgumentOutOfRangeException>(() => c.WithRed(red));
	}

	[Fact]
	public void WithRed_Double_RedNegativeShouldThrowAgumentOutOfRangeException()
	{
		Color c = new();
		var red = -Random.Shared.NextDouble();

		Assert.Throws<ArgumentOutOfRangeException>(() => c.WithRed(red));
	}

	[Fact]
	public void WithRedNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.WithRed(null, 0));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void WithGreen_Double(ColorTestDefinition testDef)
	{
		var green = Random.Shared.NextDouble();
		var result = testDef.Color.WithGreen(green);

		Assert.Equal((float)green, result.Green);
	}

	[Fact]
	public void WithGreen_Double_GreenGeaterThan1ShouldThrowAgumentOutOfRangeException()
	{
		Color c = new();
		var green = Random.Shared.Next(2, int.MaxValue);

		Assert.Throws<ArgumentOutOfRangeException>(() => c.WithGreen(green));
	}

	[Fact]
	public void WithGreen_Double_GreenNegativeShouldThrowAgumentOutOfRangeException()
	{
		Color c = new();
		var green = -Random.Shared.NextDouble();

		Assert.Throws<ArgumentOutOfRangeException>(() => c.WithGreen(green));
	}

	[Fact]
	public void WithGreenNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.WithGreen(null, 0));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void WithBlue_Double(ColorTestDefinition testDef)
	{
		var blue = Random.Shared.NextDouble();
		var result = testDef.Color.WithBlue(blue);

		Assert.Equal((float)blue, result.Blue);
	}

	[Fact]
	public void WithBlue_Double_BlueGeaterThan1ShouldThrowAgumentOutOfRangeException()
	{
		Color c = new();

		var blue = Random.Shared.Next(2, int.MaxValue);

		Assert.Throws<ArgumentOutOfRangeException>(() => c.WithBlue(blue));
	}

	[Fact]
	public void WithBlue_Double_BlueNegativeShouldThrowAgumentOutOfRangeException()
	{
		Color c = new();

		var blue = -Random.Shared.NextDouble();

		Assert.Throws<ArgumentOutOfRangeException>(() => c.WithBlue(blue));
	}

	[Fact]
	public void WithBlueNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.WithBlue(null, 0));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void WithRed_Byte(ColorTestDefinition testDef)
	{
		var red = (byte)Random.Shared.Next(0, 256);
		var result = testDef.Color.WithRed(red);

		Assert.Equal(red, result.GetByteRed());
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void WithGreen_Byte(ColorTestDefinition testDef)
	{
		var green = (byte)Random.Shared.Next(0, 256);
		var result = testDef.Color.WithGreen(green);

		Assert.Equal(green, result.GetByteGreen());
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void WithBlue_Byte(ColorTestDefinition testDef)
	{
		var blue = (byte)Random.Shared.Next(0, 256);
		var result = testDef.Color.WithBlue(blue);

		Assert.Equal(blue, result.GetByteBlue());
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void WithCyan(ColorTestDefinition testDef)
	{
		var pctCyan = Random.Shared.NextDouble();
		var newColor = testDef.Color.WithCyan(pctCyan);

		var expectedRed = (1 - pctCyan) * (1 - testDef.ExpectedPctBlack);

		Assert.Equal(Math.Round(expectedRed, 2), Math.Round(newColor.Red, 2));
		Assert.Equal(Math.Round(testDef.G, 2), Math.Round(newColor.Green, 2));
		Assert.Equal(Math.Round(testDef.B, 2), Math.Round(newColor.Blue, 2));
		Assert.Equal(Math.Round(testDef.A, 2), Math.Round(newColor.Alpha, 2));
	}

	[Fact]
	public void WithCyanNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.WithCyan(null, 0));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void WithMagenta(ColorTestDefinition testDef)
	{
		var pctMagenta = Random.Shared.NextDouble();
		var newColor = testDef.Color.WithMagenta(pctMagenta);

		var expectedGreen = (1 - pctMagenta) * (1 - testDef.ExpectedPctBlack);

		Assert.Equal(Math.Round(expectedGreen, 2), Math.Round(newColor.Green, 2));
		Assert.Equal(Math.Round(testDef.R, 2), Math.Round(newColor.Red, 2));
		Assert.Equal(Math.Round(testDef.B, 2), Math.Round(newColor.Blue, 2));
		Assert.Equal(Math.Round(testDef.A, 2), Math.Round(newColor.Alpha, 2));
	}

	[Fact]
	public void WithMagentaNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.WithMagenta(null, 0));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void WithYellow(ColorTestDefinition testDef)
	{
		var pctYellow = Random.Shared.NextDouble();
		var newColor = testDef.Color.WithYellow(pctYellow);

		var expectedBlue = (1 - pctYellow) * (1 - testDef.ExpectedPctBlack);

		Assert.Equal(Math.Round(expectedBlue, 2), Math.Round(newColor.Blue, 2));
		Assert.Equal(Math.Round(testDef.R, 2), Math.Round(newColor.Red, 2));
		Assert.Equal(Math.Round(testDef.G, 2), Math.Round(newColor.Green, 2));
		Assert.Equal(Math.Round(testDef.A, 2), Math.Round(newColor.Alpha, 2));
	}

	[Fact]
	public void WithYellowNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.WithYellow(null, 0));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorTestData))]
	public void WithBlackKey(ColorTestDefinition testDef)
	{
		var pctBlack = Random.Shared.NextDouble();
		var newColor = testDef.Color.WithBlackKey(pctBlack);

		var expectedRed = (1 - testDef.ExpectedPctCyan) * (1 - pctBlack);
		var expectedGreen = (1 - testDef.ExpectedPctMagenta) * (1 - pctBlack);
		var expectedBlue = (1 - testDef.ExpectedPctYellow) * (1 - pctBlack);

		Assert.Equal(Math.Round(expectedRed, 2), Math.Round(newColor.Red, 2));
		Assert.Equal(Math.Round(expectedGreen, 2), Math.Round(newColor.Green, 2));
		Assert.Equal(Math.Round(expectedBlue, 2), Math.Round(newColor.Blue, 2));
		Assert.Equal(testDef.A, newColor.Alpha);
	}

	[Fact]
	public void WithBlackKeyNullInput()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ColorConversionExtensions.WithBlackKey(null, 0));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}


	[Theory]
	[InlineData(-1, 0)]
	[InlineData(256, 255)]
	public void ColorRange(int requestColor, int realColor)
	{
		var newColor = new Color(requestColor, requestColor, requestColor);
		Assert.Equal(realColor, newColor.GetByteRed());
		Assert.Equal(realColor, newColor.GetByteBlue());
		Assert.Equal(realColor, newColor.GetByteGreen());
	}

	public class ColorTestDefinition
	{
		internal ColorTestDefinition(float r, float g, float b, float a,
			byte expectedByteR, byte expectedByteG, byte expectedByteB, byte expectedByteA,
			float expectedPctBlack, float expectedPctCyan, float expectedPctMagenta, float expectedPctYellow,
			float expectedInverseR, float expectedInverseG, float expectedInverseB,
			float expectedAvgColor, bool expectedIsDark, bool expectedIsDarkForEye,
			Color expectedToBlackOrWhite, Color expectedToBlackOrWhiteForText)
		{
			R = r;
			G = g;
			B = b;
			A = a;

			ExpectedByteR = expectedByteR;
			ExpectedByteG = expectedByteG;
			ExpectedByteB = expectedByteB;
			ExpectedByteA = expectedByteA;
			ExpectedPctBlack = expectedPctBlack;
			ExpectedPctCyan = expectedPctCyan;
			ExpectedPctMagenta = expectedPctMagenta;
			ExpectedPctYellow = expectedPctYellow;
			ExpectedInverseR = expectedInverseR;
			ExpectedInverseG = expectedInverseG;
			ExpectedInverseB = expectedInverseB;
			ExpectedAvgColor = expectedAvgColor;
			ExpectedIsDark = expectedIsDark;
			ExpectedIsDarkForEye = expectedIsDarkForEye;
			ExpectedToBlackOrWhite = expectedToBlackOrWhite;
			ExpectedToBlackOrWhiteForText = expectedToBlackOrWhiteForText;
			ExpectedGreyScale = new Color(expectedAvgColor, expectedAvgColor, expectedAvgColor);
			ExpectedInverse = new Color(expectedInverseR, expectedInverseG, expectedInverseB);
			ExpectedRgb = $"RGB({expectedByteR},{expectedByteG},{expectedByteB})";
			ExpectedRgba = $"RGBA({expectedByteR},{expectedByteG},{expectedByteB},{A})";
			ExpectedHexrgb = $"#{expectedByteR:X2}{expectedByteG:X2}{expectedByteB:X2}";
			ExpectedHexrgba = $"#{expectedByteR:X2}{expectedByteG:X2}{expectedByteB:X2}{expectedByteA:X2}";
			ExpectedHexargb = $"#{expectedByteA:X2}{expectedByteR:X2}{expectedByteG:X2}{expectedByteB:X2}";
			ExpectedCmyk = $"CMYK({expectedPctCyan:P0},{expectedPctMagenta:P0},{expectedPctYellow:P0},{expectedPctBlack:P0})";
			ExpectedCmyka = $"CMYKA({expectedPctCyan:P0},{expectedPctMagenta:P0},{expectedPctYellow:P0},{expectedPctBlack:P0},{a})";

			Color = new Color(r, g, b, a);
			ExpectedDegreeHue = Color.GetHue() * 360;
			ExpectedHslString = $"HSL({ExpectedDegreeHue:0},{Color.GetSaturation():P0},{Color.GetLuminosity():P0})";
			ExpectedHslaString = $"HSLA({ExpectedDegreeHue:0},{Color.GetSaturation():P0},{Color.GetLuminosity():P0},{a})";
		}

		internal float R { get; }
		internal float G { get; }
		internal float B { get; }
		internal float A { get; }
		internal Color Color { get; }
		internal byte ExpectedByteR { get; }
		internal byte ExpectedByteG { get; }
		internal byte ExpectedByteB { get; }
		internal byte ExpectedByteA { get; }
		internal float ExpectedPctBlack { get; }
		internal float ExpectedPctCyan { get; }
		internal float ExpectedPctMagenta { get; }
		internal float ExpectedPctYellow { get; }
		internal float ExpectedInverseR { get; }
		internal float ExpectedInverseG { get; }
		internal float ExpectedInverseB { get; }
		internal float ExpectedAvgColor { get; }
		internal bool ExpectedIsDark { get; }
		internal bool ExpectedIsDarkForEye { get; }
		internal Color ExpectedToBlackOrWhite { get; }
		internal Color ExpectedToBlackOrWhiteForText { get; }
		internal Color ExpectedGreyScale { get; }
		internal Color ExpectedInverse { get; }
		internal string ExpectedRgb { get; }
		internal string ExpectedRgba { get; }
		internal string ExpectedHexrgb { get; }
		internal string ExpectedHexrgba { get; }
		internal string ExpectedHexargb { get; }
		internal string ExpectedCmyk { get; }
		internal string ExpectedCmyka { get; }
		internal string ExpectedHslString { get; }
		internal string ExpectedHslaString { get; }
		internal double ExpectedDegreeHue { get; }
	}
}