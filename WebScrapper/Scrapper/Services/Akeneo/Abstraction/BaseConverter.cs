namespace WebApplication.Scrapper.Services.Akeneo.Abstraction
{
    public abstract class BaseConverter<TDto, TData>
    {
        public virtual TData ConvertToApplicationEntity(TDto dto)
        {
            return ConvertToApplicationEntity(dto);
        }

        public virtual TDto ConvertToDtoEntity(TData data)
        {
            return ConvertToDtoEntity(data);
        }
    }
}