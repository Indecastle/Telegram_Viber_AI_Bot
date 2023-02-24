using System.ComponentModel.DataAnnotations;

namespace Telegram_AI_Bot.Core.Models;

public class FieldLength
{
    public class SmAttribute : StringLengthAttribute
    {
        public SmAttribute() : base(ModelSettings.SmField)
        {
        }
    }

    public class MdAttribute : StringLengthAttribute
    {
        public MdAttribute() : base(ModelSettings.MdField)
        {
        }
    }

    public class LgAttribute : StringLengthAttribute
    {
        public LgAttribute() : base(ModelSettings.LgField)
        {
        }
    }

    public class XlAttribute : StringLengthAttribute
    {
        public XlAttribute() : base(ModelSettings.XlField)
        {
        }
    }
}