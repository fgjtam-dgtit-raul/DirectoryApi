using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthApi.Helper
{
    public class EmailTemplates
    {

        public static string ValidationEmail(string href){
            return @"<body style='margin:2rem auto;width:36rem'><p>¡Gracias por registrarte en la Fiscalía Digital del Estado de Tamaulipas! Estás a solo un paso de completar la creación de tu cuenta, que será tu <b>llave digital</b> para acceder a todos nuestros servicios en línea.</p><p>Para continuar, necesitamos que valides tu dirección de correo electrónico. Esta validación es importante, ya que te permitirá recibir notificaciones y actualizaciones importantes sobre tus trámites y servicios.</p><p>Por favor, haz clic en el siguiente enlace para confirmar tu correo electrónico y continuar con la captura de tus datos personales:</p><div style='display:flex;justify-items:center;margin-top:2rem'><a href='{urlRef}' target='_blank' style='margin:0 auto;display:flex;align-items:center;justify-content:center;background-color:#627a8b;padding:.25rem 1rem;border:1px solid #566977;border-radius:.25rem;color:white;cursor:pointer;box-shadow:#00000033 0px 2px 4px 1px;text-decoration:none'><span style='margin:0rem 0.5rem;text-transform:uppercase;font-size:1rem'>Continuar</span></a></div><p style='margin-top:2rem'>Estamos comprometidos en ofrecerte un servicio eficiente y seguro. Completa este último paso y empieza a aprovechar todas las ventajas de la Fiscalía Digital.</p><p style='margin-top:2rem'><center>Atentamente</center><b style='padding-top:0.1rem'><center>Fiscalía General de Justicia del Estado de Tamaulipas</center></b></p></body>".Replace("{urlRef}", href);
        }

        public static string ResetPassword(string href){
            return "<body><table width='100%' border='0' cellspacing='0' cellpadding='0'><tr><td align='center' style='padding: 20px;'><table class='content' width='600' border='0' cellspacing='0' cellpadding='0' style='border-collapse: collapse;'><tr><td class='body' style='padding: 40px; text-align: left; font-size: 16px; line-height: 1.6;'>Para restablecer su contraseña, siga el enlace siguiente:</td></tr><tr><td style='padding: 0px 40px 0px 40px; text-align: center;'><table cellspacing='0' cellpadding='0' style='margin: auto;'><tr><td align='center' style='background-color: #345C72; padding: 10px 20px; border-radius: 5px;'><a href='{{urlRef}}' target='_blank' style='color: #ffffff; text-decoration: none; font-weight: bold;'>Restablecer contraseña</a></td></tr></table></td></tr><tr><td class='body' style='padding: 40px; text-align: left; font-size: 16px; line-height: 1.6;'>Si tiene alguna pregunta o necesita más ayuda, no dude en ponerse en contacto con nuestro equipo de asistencia.</td></tr></table></td></tr></table></body>".Replace("{{urlRef}}", href);
        }
        public static string ResetPasswordCode(string code, string time){
            return @"<body>
                <table class='content' width='600' border='0' cellspacing='0' cellpadding='0' style='margin:0 auto;border-collapse:collapse;'>
                    <tr><td class='body' style='padding:20px 40px;text-align:left;font-size:16px;line-height:1.6;'>
                        <p>Hemos recibido su solicitud para restablecer la contraseña de su llave digital. Para completar el proceso, por favor utilice el siguiente código de verificación:</p>
                    </td></tr>
                    <tr><td style='padding:10px 20px;'>
                        <div style='margin:0 auto;padding:10px 20px 10px 45px;background-color:#345C72;border-radius:5px;width:fit-content;text-align:center;font-size:1.75rem;color:#ffffff;text-decoration:none;font-weight:bold;letter-spacing:1.5rem;font-family:consolas,monospace;'>{{code}}</div>
                    </td></tr>
                    <tr><td class='body' style='padding:10px 40px;text-align:left;font-size:16px;line-height:1.6;'>
                        <p>Este código es válido por un tiempo limitado y deberá utilizarlo antes de las {{time}}. Si no realiza el restablecimiento antes de esta hora, tendrá que solicitar un nuevo código.</p>
                    </td></tr>
                    <tr><td class='body' style='padding:10px 40px;text-align:left;font-size:16px;line-height:1.6;'>
                        <b>Importante: </b>
                        <ul>
                            <li>No comparta este código con nadie más.</li>
                            <li>El personal de la Fiscalía nunca le pedirá su contraseña ni este código de verificación.</li>
                        </ul>
                    </td></tr>
                    <tr><td class='body' style='padding:10px 40px;text-align:left;font-size:16px;line-height:1.6;'>
                        <p>Si usted no solicitó el restablecimiento de su contraseña, puede ignorar este correo electrónico. Su cuenta permanecerá segura y sin cambios.</p>
                    </td></tr>
                </table>
                <div style='margin-top:2rem;text-align:center;'>Atentamente</div>
                <b style='padding-top:0.2rem;text-align:center;'>Fiscalía General de Justicia del Estado de Tamaulipas</b> </body>".Replace("{{code}}", code).Replace("{{time}}", time);;
        }

        public static string Welcome(string personFullName, string imageNameSrc, string imageProfileSrc, string welcomeMessage){
            // welcomeMessage = "¡Bienvenido(a) a la Fiscalía Digital!"
            return @"<body style='margin:2rem auto;width:40rem'><h2 style='text-align:center'>{welcome-message}<br/>{user-name}</h2><p>Nos complace informarle que su cuenta recién creada es mucho más que un simple registro, es su <b>llave digital</b> para acceder a una amplia gama de servicios ofrecidos por la Fiscalía General de Justicia del Estado de Tamaulipas.</p><p>Con esta llave digital, podrá:</p><ul><li><b>Presentar denuncias en línea</b> de manera rápida y segura.</li><li><b>Obtener constancias de antecedentes penales</b> sin tener que desplazarse.</li><li><b>Reportar el extravío de documentos</b> desde cualquier lugar.</li><li><b>Localizar oficinas</b> de la Fiscalía con facilidad.</li><li><b>Presentar quejas en línea</b> contra servidores públicos de la Fiscalía.</li></ul><p>Su cuenta le permite gestionar todos estos servicios desde un solo lugar, brindándole la comodidad de acceder a la justicia y a la información que necesita, cuando la necesita.</p><p style='margin-top:1rem'>Puede actualizar sus datos personales en cualquier momento haciendo clic en su nombre, asegurándose de que su llave digital siempre esté al día.</p><img style='width:36rem;margin:.25rem auto;background:#627a8b;padding:.25rem;border:1px solid #566977;border-radius:.25rem;box-shadow:#00000033 0px 2px 12px 1px' src='{image-name}' alt='imagen descriptiva opcion perfil'/><p style='margin-top:2rem'>Además, tiene acceso a un apartado donde podrá consultar el historial de todos sus trámites.</p><img style='width:36rem;margin:.25rem auto;background:#627a8b;padding:.25rem;border:1px solid #566977;border-radius:.25rem;box-shadow:#00000033 0px 2px 12px 1px' src='{image-profile}' alt='imagen descriptiva opcion consulta tramites'/><p style='margin-top:2rem'>Nuestro compromiso es brindarle un servicio eficiente y transparente, asegurando que sus derechos sean protegidos y que la justicia esté al alcance de todos.</p><p><b>Gracias por confiar en nosotros.</b></p><div style='display:flex;justify-items:center;margin-top:2rem'><a href='https://fiscaliadigital.fgjtam.gob.mx/mi-perfil' style='margin:0 auto;display:flex;align-items:center;justify-content:center;background-color:#627a8b;padding:.25rem 1rem;border:1px solid #566977;border-radius:.25rem;color:white;cursor:pointer;box-shadow:#00000033 0px 2px 4px 1px;text-decoration:none'><span style='text-transform:uppercase;font-size:1rem'>Ver mi perfil</span></a></div><p style='margin-top:2rem'><center>Atentamente</center><b style='padding-top:.1rem'><center>Fiscalía General de Justicia del Estado de Tamaulipas</center></b></p></body>"
                .Replace("{user-name}", personFullName)
                .Replace("{image-name}", imageNameSrc)
                .Replace("{image-profile}", imageProfileSrc)
                .Replace("{welcome-message}", welcomeMessage);
        }

        public static string CodeChangeEmail(string code, string time){
            return @"<body>
            <table style='margin:0 auto;border-collapse:collapse;' class='content' width='600' border='0' cellspacing='0' cellpadding='0'>
                <tr><td class='body' style='padding:10px 40px;text-align:left;font-size:16px;line-height:1.6;'>
                    <p>Hemos recibido una solicitud para cambiar la dirección de correo electrónico asociada a su cuenta. Si usted ha solicitado este cambio, por favor, utilice el siguiente código de verificación para confirmar la actualización de su correo electrónico:</p>
                </td></tr>
                <tr><td style='padding:10px 20px;'>
                    <div style='margin:0 auto;padding:10px 20px 10px 45px;background-color:#345C72;border-radius:5px;width:fit-content;text-align:center;font-size:1.75rem;color:#ffffff;text-decoration:none;font-weight:bold;letter-spacing:1.5rem;font-family:consolas,monospace;'>{code}</div>
                </td></tr>
                <tr><td class='body' style='padding:10px 40px;text-align:left;font-size:16px;line-height:1.6;'>
                    <p>Si no ha solicitado un cambio de correo electrónico, ignore este mensaje. Por su seguridad, este código caducará a las {time}.</p>
                </td></tr>
                <tr><td class='body' style='padding:0px 40px;text-align:left;font-size:16px;line-height:1.6;'>
                    <b>Importante: </b>
                    <ul>
                        <li>No comparta este código con nadie más.</li>
                        <li>El personal de la Fiscalía nunca le pedirá su contraseña ni este código de verificación.</li>
                    </ul>
                </td></tr>
            </table>
            <div style='margin-top:2rem;text-align:center;'>Atentamente</div>
            <b style='padding-top:0.2rem;text-align:center;'>Fiscalía General de Justicia del Estado de Tamaulipas</b>
            </body>".Replace("{code}", code).Replace("{time}", time);
        }
    }
}